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
    Climb = 5
}

public class PlayerController : MonoBehaviour
{
    private PlayerState currentState = PlayerState.Movement;
    private AttackDirection currentAttackDirection = AttackDirection.None;

    Vector3 flippedScale = new Vector3(-1, 1, 1);

    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private AnimationCurve jumpForceCurve;

    [SerializeField] private float dashForce = 10f;

    private bool canJumpTwice = true; //是否可以二段跳
    private bool canDash = true; //是否可以冲刺
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashDuration = 0.2f;

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
        currentState = PlayerState.Movement;
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
        }
    }

    private void AttackMove()
    {
        //攻击移动逻辑
        transform.position += new Vector3(moveX * speed * 0.5f * Time.deltaTime, 0, 0);
    }

    private void Movement()
    {
        rb.velocity = new Vector2(moveX * speed, rb.velocity.y);

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

        GameObject fireBallObj = Instantiate(fireBallPrefabAsset, fireBallSpawnPoint.position, fireBallSpawnPoint.rotation);
        FireBall fireBall = fireBallObj.GetComponent<FireBall>();
        if (fireBall == null)
        {
            Debug.LogWarning("火球预制体缺少 FireBall 组件");
            return;
        }

        fireBall.Initialize(transform.localScale.x > 0 ? false : true);
    }

    private void OnFireBallAnimEnd()
    {
        currentState = PlayerState.Movement;
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
        currentState = PlayerState.Movement;
        rb.gravityScale = 1.5f; //恢复重力影响
        rb.velocity = new Vector2(0, 0);//清空所有冲刺时的速度
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


    private float jumpPressTime = 0f;
    [SerializeField] private float maxJumpPressTime = 0.8f;
    private bool isJumping = false;

    private float fall_time = 0f; //记录下落时间
    [SerializeField] private float hardLandingThreshold = 0.5f; //硬着陆阈值
    private bool hardLand = false; //是否硬着陆

    private void Jump()
    {
        bool jumpDown = (InputManager.instance != null) ? InputManager.instance.GetButtonDown(InputManager.GameButton.Jump) : Input.GetKeyDown(KeyCode.K);
        if (jumpDown)
        {
            if (isOnGround)
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

                jumpPressTime = 0f;
                canJumpTwice = true; //在地面时重置二段跳
                isJumping = true;
                hardLand = false; //重置硬着陆状态
                anim.SetBool("hard_land", hardLand);
                anim.SetTrigger("jump");
                anim.ResetTrigger("jumpTwo");
                SoundManager.instance.PlaySound(SoundIndex.player_jump);
            }
            else if (canJumpTwice)
            {
                jumpPressTime = 0f;
                canJumpTwice = false; //只能二段跳一次
                isJumping = false;
                DoubleJump();
            }
        }

        bool jumpHeld = (InputManager.instance != null) ? InputManager.instance.GetButton(InputManager.GameButton.Jump) : Input.GetKey(KeyCode.K);
        if (jumpHeld && isJumping)
        {
            jumpPressTime += Time.deltaTime;
            jumpPressTime = Mathf.Min(jumpPressTime, maxJumpPressTime);
            float jumpForceFactor = jumpForceCurve.Evaluate(jumpPressTime / maxJumpPressTime);
            rb.AddForce(new Vector2(0, jumpForce * jumpForceFactor * Time.deltaTime), ForceMode2D.Force);
        }

        bool jumpUp = (InputManager.instance != null) ? InputManager.instance.GetButtonUp(InputManager.GameButton.Jump) : Input.GetKeyUp(KeyCode.K);
        if (jumpUp)
        {
            jumpPressTime = 0f;
            isJumping = false;
            JumpCancel();
        }
    }

    [SerializeField] private float doubleJumpForce = 6f;
    private void DoubleJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0); //重置垂直速度
        rb.AddForce(new Vector2(0, doubleJumpForce), ForceMode2D.Impulse);
        anim.SetTrigger("jumpTwo");
        SoundManager.instance.PlaySound(SoundIndex.player_jump);
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
        GameObject dustEff = Instantiate(dust_effect, effectPos, Quaternion.identity);
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

    public bool IsClimbing()
    {
        return currentState == PlayerState.Climb;
    }

    //PlayerClimb通知PlayerController开始攀爬
    public void OnClimbStart()
    {
        canJumpTwice = true; //重置二段跳
        currentState = PlayerState.Climb;
    }

    //PlayerClimb通知PlayerController攀爬结束，恢复移动状态
    public void OnClimbEnd()
    {
        currentState = PlayerState.Movement;
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
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(direction * force, ForceMode2D.Impulse);
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
}
