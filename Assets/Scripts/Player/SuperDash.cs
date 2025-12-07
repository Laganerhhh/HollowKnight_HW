using System;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 超级冲刺功能
/// </summary>
public class SuperDash : MonoBehaviour
{
    public enum DashState { Idle, Charging, Waiting, Dashing, Stopping };
    private DashState currentState = DashState.Idle;
    [SerializeField] private float chargeTime = 1.0f;
    [SerializeField] private float maxChargeTime = 2.5f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashSpeed = 20.0f;
    private float chargeTimer = 0.0f;
    private float dashTimer = 0.0f;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerController playerController;

    private AudioSource audioSource;

    private AudioClip superDashSound; //冲刺音效

    private bool isClimbingSuperDash = false;

    [Header("超级冲刺特效")]
    [SerializeField] private SuperDashEff superDashEff;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        superDashSound = Resources.Load<AudioClip>("Audios/" + SoundIndex.player_superDash_loop);

        if (superDashSound == null)
        {
            Debug.LogError("冲刺音效未加载");
        }
    }

    public DashState GetDashState()
    {
        return currentState;
    }

    void Update()
    {
        switch (currentState)
        {
            case DashState.Idle:
                HandleIdleState();
                break;

            case DashState.Charging:
                HandleChargingState();
                break;

            case DashState.Waiting:
                HandleWaitingState();
                break;

            case DashState.Dashing:
                HandleDashingState();
                break;

            case DashState.Stopping:
                HandleStoppingState();
                break;
        }
    }

    public bool IsSuperDashing()
    {
        return currentState == DashState.Dashing;
    }

    private void ChangeState(DashState newState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    private void EnterState(DashState newState)
    {
        switch (newState)
        {
            case DashState.Idle:
                playerController.enabled = true;
                break;

            case DashState.Charging:
                playerController.enabled = false;
                rb.gravityScale = 0;
                chargeTimer = 0.0f;
                animator.SetTrigger("superDash");
                SoundManager.instance.PlaySound(SoundIndex.player_superDash_charge);
                break;

            case DashState.Waiting:
                rb.gravityScale = 0;
                break;

            case DashState.Dashing:
                //如果是在墙壁上，则会朝远离墙壁的方向冲刺
                if (isClimbingSuperDash)
                {
                    //改变角色朝向
                    transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
                    isClimbingSuperDash = false;
                }
                dashTimer = 0.0f;
                rb.gravityScale = 0;
                animator.SetTrigger("superDash_sprint");
                //播放冲刺音效（循环）
                audioSource.clip = superDashSound;
                audioSource.loop = true;
                audioSource.Play();
                break;

            case DashState.Stopping:
                rb.velocity = new Vector2(0, 0);
                rb.gravityScale = 1.5f;
                animator.SetTrigger("superDash_stop");
                break;
        }
    }

    private void ExitState(DashState oldState)
    {
        switch (oldState)
        {
            case DashState.Idle:
                playerController.enabled = false;
                rb.velocity = new Vector2(0, 0);
                break;

            case DashState.Charging:
                chargeTimer = 0.0f;
                isPlayingReadySound = false;
                rb.gravityScale = 1.5f;
                break;

            case DashState.Waiting:
                chargeTimer = 0.0f;
                rb.gravityScale = 1.5f;
                break;

            case DashState.Dashing:
                dashTimer = 0.0f;
                //停止播放冲刺音效
                audioSource.Stop();
                rb.gravityScale = 1.5f;

                OnSuperDashEnd();
                break;

            case DashState.Stopping:
                break;
        }
    }

    private void HandleIdleState()
    {
        bool CanSuperDash = playerController.CanSuperDash();
        //按下I开始蓄力前摇
        if (CanSuperDash && Input.GetKeyDown(KeyCode.I))
        {
            ChangeState(DashState.Charging);
        }

    }

    private void HandleChargingState()
    {
        //等待动画播放完毕后开始蓄力等待
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if ((stateInfo.IsName("superDash_charged") || stateInfo.IsName("climb_superdash_repeat")) && stateInfo.normalizedTime >= 1.0f)
        {
            if (stateInfo.IsName("climb_superdash_repeat"))
            {
                isClimbingSuperDash = true;
            }
            ChangeState(DashState.Waiting);
        }
        //松开I键则返回Idle状态
        if (Input.GetKeyUp(KeyCode.I))
        {
            animator.SetTrigger("superDash_cancel");
            ChangeState(DashState.Idle);
        }
    }

    private bool isPlayingReadySound = false;

    private void HandleWaitingState()
    {
        chargeTimer += Time.deltaTime;

        if (chargeTimer >= chargeTime && !isPlayingReadySound)
        {
            //准备就绪
            SoundManager.instance.PlaySound(SoundIndex.player_superDash_ready);
            isPlayingReadySound = true;
        }

        if (chargeTimer >= maxChargeTime)
        {
            //达到最大蓄力时间后自动进入冲刺状态
            ChangeState(DashState.Dashing);
        }

        //等待松开按键
        if (Input.GetKeyUp(KeyCode.I))
        {
            if (chargeTimer >= chargeTime)
                ChangeState(DashState.Dashing);
            else
            {
                animator.SetTrigger("superDash_cancel");
                ChangeState(DashState.Idle);
            }
                
        }
    }

    private void HandleDashingState()
    {
        dashTimer += Time.deltaTime;
        float dashDir = transform.localScale.x * -1; //根据角色朝向决定冲刺方向
        rb.velocity = new Vector2(dashSpeed * dashDir, 0);

        if (dashTimer >= dashDuration)
        {
            ChangeState(DashState.Stopping);
        }

        if (Input.GetKeyDown(KeyCode.K)) //按下跳跃键停止冲刺
        {
            ChangeState(DashState.Stopping);
        }
    }

    private void HandleStoppingState()
    {
        //等待动画播放完毕后返回Idle状态
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("idle") || stateInfo.IsName("fall"))
        {
            //恢复玩家控制移动
            playerController.enabled = true;
            ChangeState(DashState.Idle);
        }
    }

    public void ResetAll()
    {
        ChangeState(DashState.Idle);
        audioSource.Stop();
        animator.ResetTrigger("superDash");
        EnterState(DashState.Idle);
    }

    private void OnSuperDashStart()
    {
        superDashEff.OnSuperDashStart();
    }

    private void OnSuperDashEnd()
    {
        superDashEff.OnSuperDashEnd();
    }
}
