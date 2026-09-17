using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// 玩家控制脚本
/// </summary>
/// 

public enum AttackDirection
{
    None = 0,
    LeftRight = 1,
    Up = 2,
    Down = 3
}

enum PlayerState
{
    Movement = 0,
    Dash = 1,
    Attack = 2,
    SuperDash = 3,
    FireBall = 4,
    Climb = 5,
    Knockback = 6
}

public class PlayerController : MonoBehaviour
{
    private PlayerState currentState = PlayerState.Movement;
    private AttackDirection currentAttackDirection = AttackDirection.None;

    Vector3 flippedScale = new Vector3(-1, 1, 1);

    [SerializeField] private float speed = 5f;

    [Header("跳跃")]
    [SerializeField] private float jumpHeight = 2.8f;
    [SerializeField] private float minJumpHeight = 1.2f;
    [SerializeField] private float jumpTimeToApex = 0.38f;
    [SerializeField] private float doubleJumpHeight = 2.2f;
    [SerializeField] private float doubleJumpTimeToApex = 0.34f;
    [SerializeField] private float fallGravityMultiplier = 1.35f;
    [SerializeField] private float jumpCutGravityMultiplier = 2.2f;
    [SerializeField] private float maxFallSpeed = 18f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    private float baseGravityScale = 1.5f;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isJumpHeld;
    private bool isJumping;
    private bool hasConsumedGroundJump;
    private float currentJumpHeight;
    private float currentJumpTimeToApex;

    [Header("攀爬跳跃适配")]
    [SerializeField] private float wallJumpHorizontalLockTime = 0.12f;
    [SerializeField] private float wallJumpHorizontalControlRatio = 0.35f;
    private float wallJumpHorizontalVelocity;
    private float wallJumpHorizontalLockEndTime;
    private int ignoreJumpPressedFrame = -1;

    [SerializeField] private float dashForce = 10f;

    [Header("反冲")]
    [SerializeField] private float knockbackDuration = 0.18f;
    [SerializeField] private float knockbackCooldown = 0.08f;
    [SerializeField] private float knockbackHorizontalControlRatio = 1f;
    [SerializeField] private float knockbackHorizontalDecay = 18f;
    private Vector2 knockbackVelocity;
    private float knockbackEndTime;
    private float nextKnockbackAcceptTime;
    private float currentKnockbackForce;
    private int lastKnockbackFrame = -1;

    private bool canJumpTwice = true; //是否可以二段跳
    private bool canDash = true; //是否可以冲刺
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashDuration = 0.2f;

    private float fall_time = 0f; //记录下落时间
    [SerializeField] private float hardLandingThreshold = 0.5f; //硬着陆阈值
    private bool hardLand = false; //是否硬着陆

    [Header("着陆特效")]
    [SerializeField] private GameObject dust_effect;

    private const string FireBallPrefabPath = "Effect/shadow_fireball.prefab";

    [Header("火球")]
    [SerializeField] private float fireBall_cooldown = 1.0f;
    private bool canFireBall = true;
    [SerializeField] private Transform fireBallSpawnPoint;
    private GameObject fireBallPrefabAsset;

    [SerializeField] private bool canAttack = true;
    [SerializeField] private float attackCooldown = 0.5f;

    private float moveX;
    private float moveY;
    private int moveChanged = 0;
    private bool isOnGround = true;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerSoulPower soulPower;
    private PlayerHealth playerHealth;
    private bool isSceneClosing;

    void OnEnable()
    {
        isSceneClosing = false;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        //重设置动画状态
        currentState = PlayerState.Movement;
        moveChanged = 0;
        anim.SetInteger("movement", moveChanged);
    }

    void OnDisable()
    {
        isSceneClosing = true;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        ResetAllParameters();
        anim.SetInteger("movement", 0);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        soulPower = GetComponent<PlayerSoulPower>();
        playerHealth = GetComponent<PlayerHealth>();
        baseGravityScale = Mathf.Max(0.01f, rb.gravityScale);
        ResetCurrentJumpProfile();
        fireBallPrefabAsset = ResourceManager.EnsureInstance().LoadPrefab(FireBallPrefabPath);
        if (fireBallPrefabAsset == null)
        {
            Debug.LogWarning($"火球预制体预加载失败: {FireBallPrefabPath}");
        }
    }

    private void OnSceneUnloaded(Scene unloadedScene)
    {
        if (unloadedScene == gameObject.scene)
        {
            isSceneClosing = true;
        }
    }

    private void OnActiveSceneChanged(Scene currentScene, Scene nextScene)
    {
        if (currentScene == gameObject.scene)
        {
            isSceneClosing = true;
        }
    }

    private void OnApplicationQuit()
    {
        isSceneClosing = true;
    }

    // 将安全点的 Y 固定为检测到的平台顶部（更安全），如果未检测到平台则使用当前坐标
    void UpdateSafePositionToPlatformTop()
    {
        if (playerHealth == null) return;
        int terrainLayer = LayerMask.NameToLayer("Terrian");
        if (terrainLayer < 0)
        {
            // 若层不存在，直接使用当前位置
            playerHealth.safePosition = transform.position;
            return;
        }

        int layerMask = 1 << terrainLayer;
        float rayDist = 1.0f;
        //要忽略自己身上的碰撞体，可以使用Raycast的layerMask参数
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayDist, layerMask);
        //debug用
        Debug.DrawRay(transform.position, Vector2.down * rayDist, Color.red, 1.0f);
        // 如果射线命中并且命中的地面属于可移动平台，则不记录安全点
        if (hit.collider != null && hit.collider.GetComponentInParent<MoveableItem>() != null)
        {
            return;
        }

        if (hit.collider != null)
        {
            float offset = 0.1f; // 置于地面上方一点，避免卡住
            playerHealth.safePosition = new Vector2(transform.position.x, transform.position.y + offset);
        }
    }

    // 使用指定的地面 Collider 来设置安全点（忽略可移动平台）
    void SetSafePositionFromCollider(Collider2D col)
    {
        if (playerHealth == null || col == null) return;

        // 如果该地面属于可移动物体（父链上有 MoveableItem），不要把它作为安全点
        if (col.GetComponentInParent<MoveableItem>() != null)
            return;

        float offset = 0.1f;
        //需要检查一下，避免穿透地面
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + 1f), Vector2.down, 1.5f);
        if (hit.collider != null && hit.collider == col)
            playerHealth.safePosition = new Vector2(transform.position.x, transform.position.y + offset);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        soulPower = GetComponent<PlayerSoulPower>();
        playerHealth = GetComponent<PlayerHealth>();
        baseGravityScale = Mathf.Max(0.01f, rb.gravityScale);
        ResetCurrentJumpProfile();

        if (!GameManager.instance.isLastLevel)
            this.transform.position = GameManager.instance.GetRespawnPoint();
        anim.SetTrigger("respawn");
    }

    private bool canCombo = false;
    // 当前接触的地面碰撞体（用于更精确地设置安全点）
    private Collider2D currentGroundCollider;
    // 在地面持续多长时间后才自动更新安全点，避免第一帧就记录移动平台位置
    [SerializeField] private float minGroundedTimeForSafe = 0.05f;
    private float groundedTime = 0f;

    private void HandleMovementInput()
    {
        //处理移动输入逻辑
        moveX = Input.GetAxis("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");
    }

    private void HandleDashInput()
    {
        //处理冲刺输入逻辑
        bool dashInput = (InputManager.instance != null) ? InputManager.instance.GetButtonDown(InputManager.GameButton.Dash) : Input.GetKeyDown(KeyCode.L);
        if (dashInput && canDash)
        {
            currentState = PlayerState.Dash;
        }
    }

    private void HandleAttackInput()
    {
        //处理攻击输入逻辑
        bool attackInput = (InputManager.instance != null) ? InputManager.instance.GetButtonDown(InputManager.GameButton.Attack) : Input.GetKeyDown(KeyCode.J);
        if (attackInput && canAttack)
        {
            currentState = PlayerState.Attack;
            //根据按键方向确定攻击方向
            if (moveY > 0)
            {
                currentAttackDirection = AttackDirection.Up;
            }
            else if (moveY < 0)
            {
                currentAttackDirection = AttackDirection.Down;
            }
            else
            {
                currentAttackDirection = AttackDirection.LeftRight;
            }

            if (!canAttack) return;

            if (currentAttackDirection == AttackDirection.LeftRight)
            {
                //水平方向有连击
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("attack_1") &&
                !anim.GetCurrentAnimatorStateInfo(0).IsName("attack_2"))
                {
                    // 不在攻击状态中，开始第一招
                    anim.SetTrigger("attack");
                    anim.SetInteger("attack_dir", (int)currentAttackDirection);
                    rb.velocity = new Vector2(0, rb.velocity.y); //攻击时水平速度为0

                }
                else if (canCombo)
                {
                    // 在连击窗口内，触发第二招
                    anim.SetBool("attack_twice", true);
                    anim.SetInteger("attack_dir", (int)currentAttackDirection);
                    canCombo = false;
                }
            }
            else
            {
                //上下方向无连击
                anim.SetTrigger("attack");
                anim.SetInteger("attack_dir", (int)currentAttackDirection);
                canAttack = false;
            }
        }
    }


    private void OnAttackStart()
    {
        //随机播放5个攻击音效
        int soundIndex = Random.Range(1, 6);
        SoundManager.instance.PlaySound(SoundIndex.player_sword + soundIndex);
    }
    public void OnAttackEnd()
    {
        //攻击结束后恢复移动状态
        if (currentState != PlayerState.Knockback)
        {
            currentState = PlayerState.Movement;
        }
        anim.SetBool("attack_twice", false);
        currentAttackDirection = AttackDirection.None;
        anim.SetInteger("attack_dir", (int)AttackDirection.None);
        StartCoroutine(AttackCooldown(attackCooldown));
    }

    IEnumerator AttackCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }

    //处理所有输入
    private void HandleInput()
    {
        HandleMovementInput();
        if (currentState == PlayerState.Knockback)
        {
            return;
        }

        HandleDashInput();
        HandleAttackInput();
        HandleFireBallInput();
    }

    void Update()
    {
        HandleInput();
        switch (currentState)
        {
            case PlayerState.Movement:
                //处理移动状态的逻辑
                Movement();
                Direction();
                Jump();
                break;
            case PlayerState.Dash:
                //处理冲刺状态的逻辑
                Dash();
                break;
            case PlayerState.Attack:
                //处理攻击状态的逻辑
                AttackMove();
                break;
            case PlayerState.SuperDash:
                //处理超级冲刺状态的逻辑

                break;
            case PlayerState.FireBall:
                //处理火球状态的逻辑
                break;

            case PlayerState.Climb:
                //处理攀爬状态的逻辑
                break;
            case PlayerState.Knockback:
                //反冲期间只允许水平输入修正位置，不处理跳跃、攻击、冲刺等垂直/动作输入
                KnockbackMove();
                Direction();
                break;
        }
    }

    private void AttackMove()
    {
        //攻击移动逻辑
        transform.position += new Vector3(moveX * speed * 0.5f * Time.deltaTime, 0, 0);
    }

    private void Movement()
    {
        float horizontalSpeed = moveX * speed;
        if (!isOnGround && Time.time < wallJumpHorizontalLockEndTime)
        {
            horizontalSpeed = wallJumpHorizontalVelocity + moveX * speed * wallJumpHorizontalControlRatio;
        }

        rb.velocity = new Vector2(horizontalSpeed, rb.velocity.y);

        if (moveX > 0)
        {
            moveChanged = 1;
        }
        else if (moveX < 0)
        {
            moveChanged = -1;
        }
        else
        {
            moveChanged = 0;
        }

        anim.SetInteger("movement", moveChanged);

        // 在地面时根据当前 ground collider 延时记录安全点，避免在刚落地第一帧就记录
        if (isOnGround)
        {
            if (currentGroundCollider != null)
            {
                groundedTime += Time.deltaTime;
                if (groundedTime >= minGroundedTimeForSafe)
                {
                    SetSafePositionFromCollider(currentGroundCollider);
                }
            }
            else
            {
                // fallback: try raycast-based update (keeps previous behavior)
                groundedTime += Time.deltaTime;
                if (groundedTime >= minGroundedTimeForSafe)
                    UpdateSafePositionToPlatformTop();
            }
        }
        else
        {
            groundedTime = 0f;
        }


        //添加下落时间
        if (!isOnGround && rb.velocity.y < 0)
        {
            fall_time += Time.deltaTime;
            if (fall_time > hardLandingThreshold)
            {
                hardLand = true;
                anim.SetBool("hard_land", hardLand);
            }
        }
    }

    private void Direction()
    {
        if (moveX > 0)
        {
            if (transform.localScale != flippedScale)
            {
                transform.localScale = flippedScale;
                anim.SetTrigger("rotate");
            }
        }
        else if (moveX < 0)
        {
            if (transform.localScale != Vector3.one)
            {
                transform.localScale = Vector3.one;
                anim.SetTrigger("rotate");
            }
        }
    }

    private void KnockbackMove()
    {
        float controlledHorizontalSpeed = moveX * speed * knockbackHorizontalControlRatio;
        float finalHorizontalSpeed = knockbackVelocity.x + controlledHorizontalSpeed;
        rb.velocity = new Vector2(finalHorizontalSpeed, rb.velocity.y);
        knockbackVelocity.x = Mathf.MoveTowards(knockbackVelocity.x, 0f, knockbackHorizontalDecay * Time.deltaTime);

        if (moveX > 0)
        {
            moveChanged = 1;
        }
        else if (moveX < 0)
        {
            moveChanged = -1;
        }
        else
        {
            moveChanged = 0;
        }
        anim.SetInteger("movement", moveChanged);

        if (Time.time >= knockbackEndTime)
        {
            FinishKnockback();
        }
    }

    private void FinishKnockback()
    {
        if (currentState != PlayerState.Knockback)
        {
            return;
        }

        currentState = PlayerState.Movement;
        knockbackVelocity = Vector2.zero;
        currentKnockbackForce = 0f;
    }


    private void HandleFireBallInput()
    {
        bool fireInput = (InputManager.instance != null) ? InputManager.instance.GetButtonDown(InputManager.GameButton.FireBall) : Input.GetKeyDown(KeyCode.U);
        if (fireInput && canFireBall && anim.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            if (!soulPower.UseSoulPower(SoulPowerSkill.FireBall))
                return;
            //发射火球
            currentState = PlayerState.FireBall;
            anim.SetTrigger("fireball");
            canFireBall = false;
            //播放火球音效
            SoundManager.instance.PlaySound(SoundIndex.player_fireball);

            StartCoroutine(FireBallCooldown(fireBall_cooldown));         
        }
    }

    IEnumerator FireBallCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canFireBall = true;
    }

    private void OnFireBall()
    {
        //发射火球逻辑
        if (fireBallSpawnPoint == null)
        {
            Debug.LogWarning("火球发射点未设置");
            return;
        }

        if (fireBallPrefabAsset == null)
        {
            Debug.LogWarning($"火球预制体未预加载: {FireBallPrefabPath}");
            return;
        }

        GameObject fireBallObj = global::ProjectilePoolManager.Spawn(fireBallPrefabAsset, fireBallSpawnPoint.position, fireBallSpawnPoint.rotation);
        if (fireBallObj == null)
        {
            fireBallObj = Instantiate(fireBallPrefabAsset, fireBallSpawnPoint.position, fireBallSpawnPoint.rotation);
        }

        FireBall fireBall = fireBallObj != null ? fireBallObj.GetComponent<FireBall>() : null;
        if (fireBall == null)
        {
            Debug.LogWarning("火球预制体缺少 FireBall 组件");
            return;
        }

        fireBall.Initialize(transform.localScale.x > 0 ? false : true);
    }

    private void OnFireBallAnimEnd()
    {
        if (currentState != PlayerState.Knockback)
        {
            currentState = PlayerState.Movement;
        }
    }

    private void Dash()
    {
        if (!canDash) return;
        //冲刺逻辑
        canDash = false; //只能冲刺一次，需在地面重置
        rb.velocity = new Vector2(0, 0); //重置当前速度
        float dashForceDir = transform.localScale.x > 0 ? -1 : 1;
        rb.AddForce(new Vector2(dashForce * dashForceDir, 0), ForceMode2D.Impulse);
        //冲刺时忽略重力影响
        rb.gravityScale = 0;
        anim.SetTrigger("dash");
        //播放冲刺音效
        SoundManager.instance.PlaySound(SoundIndex.player_dash);
        //使用协程处理冲刺持续时间和结束后的状态恢复
        StartCoroutine(DashCoroutine(dashDuration));
    }

    //冲刺协程
    IEnumerator DashCoroutine(float dashDuration = 0.2f)
    {
        yield return new WaitForSeconds(dashDuration);
        if (currentState == PlayerState.Dash)
        {
            currentState = PlayerState.Movement;
            rb.gravityScale = baseGravityScale; //恢复重力影响
            rb.velocity = new Vector2(0, 0);//清空所有冲刺时的速度
        }
        StartCoroutine(DashCooldown(dashCooldown));
    }

    IEnumerator DashCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canDash = true;
    }



    private void EnableCombo()
    {
        canCombo = true;
    }

    private void DisableCombo()
    {
        canCombo = false;
    }


    private void Jump()
    {
        UpdateJumpTimers();

        bool ignoreJumpPressedThisFrame = Time.frameCount == ignoreJumpPressedFrame;
        bool jumpDown = !ignoreJumpPressedThisFrame && ((InputManager.instance != null) ? InputManager.instance.GetButtonDown(InputManager.GameButton.Jump) : Input.GetKeyDown(KeyCode.K));
        if (jumpDown)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        isJumpHeld = (InputManager.instance != null) ? InputManager.instance.GetButton(InputManager.GameButton.Jump) : Input.GetKey(KeyCode.K);

        bool jumpUp = !ignoreJumpPressedThisFrame && ((InputManager.instance != null) ? InputManager.instance.GetButtonUp(InputManager.GameButton.Jump) : Input.GetKeyUp(KeyCode.K));
        if (jumpUp)
        {
            isJumpHeld = false;
            CutJumpByRelease();
            JumpCancel();
        }

        TryConsumeJumpBuffer();
        ApplyJumpGravity();
    }

    private void UpdateJumpTimers()
    {
        if (isOnGround)
        {
            coyoteTimer = coyoteTime;
            canJumpTwice = true;
            if (rb.velocity.y <= 0f)
            {
                isJumping = false;
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void TryConsumeJumpBuffer()
    {
        if (jumpBufferTimer <= 0f)
        {
            return;
        }

        if (coyoteTimer > 0f && !hasConsumedGroundJump)
        {
            StartJump();
        }
        else if (canJumpTwice)
        {
            StartDoubleJump();
        }
    }

    private void StartJump()
    {
        // 记录跳跃前的安全点为当前 ground collider 的平台顶部（优先），如果该平台是可移动的则不记录
        if (currentGroundCollider != null)
        {
            SetSafePositionFromCollider(currentGroundCollider);
        }
        else
        {
            UpdateSafePositionToPlatformTop();
        }

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isJumping = true;
        isJumpHeld = true;
        isOnGround = false;
        hasConsumedGroundJump = true;
        canJumpTwice = true;
        hardLand = false;
        anim.SetBool("isOnGround", isOnGround);
        anim.SetBool("hard_land", hardLand);
        anim.SetTrigger("jump");
        anim.ResetTrigger("jumpTwo");
        ResetCurrentJumpProfile();
        rb.gravityScale = GetJumpGravityScale(currentJumpHeight, currentJumpTimeToApex);
        rb.velocity = new Vector2(rb.velocity.x, GetJumpVelocity(currentJumpHeight, currentJumpTimeToApex));
        SoundManager.instance.PlaySound(SoundIndex.player_jump);
    }

    private void StartDoubleJump()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        canJumpTwice = false;
        isJumping = true;
        isJumpHeld = true;
        isOnGround = false;
        anim.SetBool("isOnGround", isOnGround);
        currentJumpHeight = doubleJumpHeight;
        currentJumpTimeToApex = doubleJumpTimeToApex;
        rb.gravityScale = GetJumpGravityScale(currentJumpHeight, currentJumpTimeToApex);
        rb.velocity = new Vector2(rb.velocity.x, GetJumpVelocity(currentJumpHeight, currentJumpTimeToApex));
        anim.SetTrigger("jumpTwo");
        SoundManager.instance.PlaySound(SoundIndex.player_jump);
    }

    private void ResetCurrentJumpProfile()
    {
        currentJumpHeight = jumpHeight;
        currentJumpTimeToApex = jumpTimeToApex;
    }

    private void CutJumpByRelease()
    {
        if (rb.velocity.y <= 0f)
        {
            return;
        }

        float minJumpVelocity = Mathf.Sqrt(2f * GetGravityMagnitude(currentJumpHeight, currentJumpTimeToApex) * minJumpHeight);
        if (rb.velocity.y > minJumpVelocity)
        {
            rb.velocity = new Vector2(rb.velocity.x, minJumpVelocity);
        }
    }

    private void ApplyJumpGravity()
    {
        if (isOnGround && rb.velocity.y <= 0f)
        {
            rb.gravityScale = baseGravityScale;
            return;
        }

        float gravityScale = GetJumpGravityScale(currentJumpHeight, currentJumpTimeToApex);
        if (rb.velocity.y < -0.01f)
        {
            gravityScale *= fallGravityMultiplier;
        }
        else if (!isJumpHeld && rb.velocity.y > 0.01f)
        {
            gravityScale *= jumpCutGravityMultiplier;
        }

        rb.gravityScale = gravityScale;
        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    private float GetJumpVelocity(float height, float timeToApex)
    {
        timeToApex = Mathf.Max(0.01f, timeToApex);
        return 2f * Mathf.Max(0.01f, height) / timeToApex;
    }

    private float GetJumpGravityScale(float height, float timeToApex)
    {
        float gravityMagnitude = GetGravityMagnitude(height, timeToApex);
        float worldGravity = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y));
        return gravityMagnitude / worldGravity;
    }

    private float GetGravityMagnitude(float height, float timeToApex)
    {
        timeToApex = Mathf.Max(0.01f, timeToApex);
        return 2f * Mathf.Max(0.01f, height) / (timeToApex * timeToApex);
    }

    //判断是否在地面

    void OnTriggerEnter2D(Collider2D collision)
    {
        Grounding(collision, false);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        Grounding(collision, false);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        Grounding(collision, true);
    }

    private void SpawnDustEffect(Collider2D col)
    {
        if (isSceneClosing || dust_effect == null || col == null)
        {
            return;
        }

        Vector2 closestPoint = col.ClosestPoint(transform.position);
        Vector3 effectPos = new Vector3(closestPoint.x, closestPoint.y - 0.1f, 0f);
        GameObject dustEff = PlayEffect(dust_effect, effectPos, Quaternion.identity, null);
        if (dustEff != null)
        {
            dustEff.transform.Rotate(new Vector3(-90f, 0f, -90f));
        }
    }

    private void Grounding(Collider2D col, bool exitState)
    {
        if (exitState)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Terrian"))
            {
                // 离开当前地面
                if (currentGroundCollider == col) currentGroundCollider = null;
                isOnGround = false;

                //离开地面创建特效
                SpawnDustEffect(col);
                anim.SetBool("isOnGround", isOnGround);
            }
        }
        else
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Terrian")
            && !isOnGround)
            {
                //在地面的一些处理
                // 记录当前接触的地面碰撞体
                currentGroundCollider = col;
                groundedTime = 0f; // 重置计时

                if (anim.GetCurrentAnimatorClipInfo(0).Length > 0 &&
                    anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "fall")
                {
                    //创建着陆特效在地面表面
                    SpawnDustEffect(col);
                    TransitionToGround();
                }

            }
            else if (col.gameObject.layer == LayerMask.NameToLayer("Terrian")
            && !isOnGround)
            {
                isOnGround = false;
                JumpCancel();
            }
        }
        anim.SetBool("isOnGround", isOnGround);
    }

    public void SetIsOnGround(bool isOnGd)
    {
        isOnGround = isOnGd;
    }

    private void TransitionToGround()
    {
        //在地面
        isOnGround = true;
        isJumping = false;
        isJumpHeld = false;
        hasConsumedGroundJump = false;
        canJumpTwice = true;
        rb.gravityScale = baseGravityScale;
        ResetCurrentJumpProfile();
        JumpCancel();
        fall_time = 0f; //重置下落时间
        // 更新玩家的最后安全点为平台顶部（更安全）
        UpdateSafePositionToPlatformTop();
    }

    private void OnLand()
    {
        SoundManager.instance.PlaySound(SoundIndex.player_softLand);
    }

    private void JumpCancel()
    {
        anim.ResetTrigger("jump");
    }

    public bool IsOnGround()
    {
        return isOnGround;
    }

    public Vector3 GetSavePosition()
    {
        if (isOnGround)
        {
            return transform.position;
        }

        return new Vector3(playerHealth.safePosition.x, playerHealth.safePosition.y, transform.position.z);
    }

    public bool IsClimbing()
    {
        return currentState == PlayerState.Climb;
    }

    public bool IsKnockbacking()
    {
        return currentState == PlayerState.Knockback;
    }

    //PlayerClimb通知PlayerController开始攀爬
    public void OnClimbStart()
    {
        canJumpTwice = true; //重置二段跳
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isJumpHeld = false;
        hasConsumedGroundJump = false;
        wallJumpHorizontalLockEndTime = 0f;
        ResetCurrentJumpProfile();
        rb.gravityScale = baseGravityScale;
        currentState = PlayerState.Climb;
    }

    //PlayerClimb通知PlayerController攀爬结束，恢复移动状态
    public void OnClimbEnd()
    {
        if (currentState != PlayerState.Knockback)
        {
            currentState = PlayerState.Movement;
            if (rb.velocity.y <= 0f)
            {
                ResetCurrentJumpProfile();
                rb.gravityScale = baseGravityScale;
            }
        }
    }

    public void StartClimbJump(Vector2 jumpVelocity, float timeToApex)
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isJumping = true;
        isJumpHeld = true;
        isOnGround = false;
        hasConsumedGroundJump = true;
        // 墙跳属于空中第一次跳跃，消耗地面跳额度，但保留二段跳额度，允许墙跳过程中再次按跳触发二段跳
        canJumpTwice = true;
        currentState = PlayerState.Movement;

        currentJumpTimeToApex = Mathf.Max(0.01f, timeToApex);
        currentJumpHeight = Mathf.Max(minJumpHeight, Mathf.Abs(jumpVelocity.y) * currentJumpTimeToApex * 0.5f);
        rb.gravityScale = GetJumpGravityScale(currentJumpHeight, currentJumpTimeToApex);
        rb.velocity = jumpVelocity;

        wallJumpHorizontalVelocity = jumpVelocity.x;
        wallJumpHorizontalLockEndTime = Time.time + wallJumpHorizontalLockTime;
        ignoreJumpPressedFrame = Time.frameCount;

        anim.SetBool("isOnGround", isOnGround);
        anim.ResetTrigger("jump");
    }

    //通知playerSuperDash可以超级冲刺
    public bool CanSuperDash()
    {
        if ((currentState == PlayerState.Movement && moveChanged == 0 && isOnGround) || currentState == PlayerState.Climb)
            return true;
        else
            return false;
    }

    /// <summary>
    /// 给玩家一个反冲的力
    /// </summary>
    public void ApplyKnockback(float force, Vector2 direction)
    {
        if (Time.frameCount == lastKnockbackFrame)
        {
            return;
        }

        if (Time.time < nextKnockbackAcceptTime && force <= currentKnockbackForce)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        direction.Normalize();
        knockbackVelocity = direction * force;
        rb.gravityScale = baseGravityScale;
        rb.velocity = knockbackVelocity;
        knockbackEndTime = Time.time + knockbackDuration;
        nextKnockbackAcceptTime = Time.time + knockbackCooldown;
        currentKnockbackForce = force;
        lastKnockbackFrame = Time.frameCount;
        currentState = PlayerState.Knockback;

        isJumping = false;
        isJumpHeld = false;
        hasConsumedGroundJump = false;
        ResetCurrentJumpProfile();
        anim.ResetTrigger("jump");
        currentAttackDirection = AttackDirection.None;
        anim.SetInteger("attack_dir", (int)AttackDirection.None);

        //反冲之后，会重置二段跳
        canJumpTwice = true;
    }

    public void ResetAllParameters()
    {
        //重置所有参数
        canAttack = true;
        canDash = true;
        canJumpTwice = true;
        currentState = PlayerState.Movement;
        moveChanged = 0;
        knockbackVelocity = Vector2.zero;
        knockbackEndTime = 0f;
        nextKnockbackAcceptTime = 0f;
        currentKnockbackForce = 0f;
        lastKnockbackFrame = -1;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        wallJumpHorizontalVelocity = 0f;
        wallJumpHorizontalLockEndTime = 0f;
        isJumpHeld = false;
        isJumping = false;
        hasConsumedGroundJump = false;
        ResetCurrentJumpProfile();
    }

    private void OnDestroy()
    {
        isSceneClosing = true;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        if (fireBallPrefabAsset != null)
        {
            BundleManager existingBundleManager = BundleManager.Instance;
            if (existingBundleManager != null)
            {
                existingBundleManager.Release(fireBallPrefabAsset);
            }

            fireBallPrefabAsset = null;
        }
    }

    private GameObject PlayEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (effectPrefab == null || isSceneClosing)
        {
            return null;
        }

        GameObject effectInstance = TryPlayEffectFromPool(effectPrefab, position, rotation, parent);
        if (effectInstance == null)
        {
            effectInstance = Instantiate(effectPrefab, position, rotation, parent);
        }

        return effectInstance;
    }

    private static GameObject TryPlayEffectFromPool(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        return EffectPoolManager.Play(effectPrefab, position, rotation, parent);
    }
}
