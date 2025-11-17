using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum ClimbState
{
    None,
    Jumping,
    Climbing,
    ClimbJumping
}

public class PlayerClimb : MonoBehaviour
{
    private PlayerController playerController;
    private Rigidbody2D rb;
    private Animator animator;

    [SerializeField] private ClimbState currentClimbState = ClimbState.None;

    private bool isJumping = false;

    public void CheckIsJumping()
    {
        isJumping = playerController.IsOnGround() ? false : true;
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        switch (currentClimbState)
        {
            case ClimbState.None:
                HandleNoneState();
                break;
            case ClimbState.Jumping:
                HandleJumpingState();
                break;
            case ClimbState.Climbing:
                HandleClimbingState();
                break;
            case ClimbState.ClimbJumping:
                HandleClimbJumpingState();
                break;
        }
    }

    private void HandleNoneState()
    {
        CheckIsJumping();
        if (isJumping)
        {
            NoneToJumping();
        }
    }

    [Header("攀爬检测")]
    public Transform leftWallCheck;
    public Transform rightWallCheck;
    public float wallCheckDistance = 0.1f; //检测距离，可以稍微比碰撞体大一点
    public LayerMask wallLayer; //指定哪些层是墙
    public Vector2 ClimbJumpForce = new Vector2(5f, 10f); //攀爬跳跃的力
    public float wallSlideSpeed = 2f; //攀爬时的下滑速度

    [SerializeField]private bool isTouchingLeftWall;
    [SerializeField]private bool isTouchingRightWall;

    private bool isFacingRight = true;

    private float inputX;
    private float inputY;

    void CheckWall()
    {
        isFacingRight = transform.localScale.x < 0;
        //从检测点向左侧发射一条很短的射线
        if (isFacingRight)
        {
            //面向右侧，左检测点检测右边墙壁
            isTouchingLeftWall = Physics2D.Raycast(rightWallCheck.position, Vector2.left, wallCheckDistance, wallLayer);
            isTouchingRightWall = Physics2D.Raycast(leftWallCheck.position, Vector2.right, wallCheckDistance, wallLayer);
        }
        else
        {
            //面向左侧，右检测点检测右边墙壁
            isTouchingLeftWall = Physics2D.Raycast(leftWallCheck.position, Vector2.left, wallCheckDistance, wallLayer);
            isTouchingRightWall = Physics2D.Raycast(rightWallCheck.position, Vector2.right, wallCheckDistance, wallLayer);
        }
    }

    private void HandleJumpingState()
    {
        CheckIsJumping();
        if (!isJumping)
        {
            JumpingToNone();
            return;
        }
        //需要射线检测，检测可不可以攀爬
        CheckWall();
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        if ((isTouchingLeftWall && inputX < -0.1f) || (isTouchingRightWall && inputX > 0.1f))
        {
            JumpingToClimbing();
        }
    }

    private void HandleClimbingState()
    {
        CheckWall();
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        //如果不再接触墙壁
        if (!isTouchingLeftWall && !isTouchingRightWall)
        {
            ClimbingToJumping();
        }
        else if (Input.GetKeyDown(KeyCode.K)) //攀爬跳跃
        {
            ClimbingToClimbJumping();
        }
        else
        {
            //什么都不做，在攀爬状态下，则会缓慢下落
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
        }
    }

    private void HandleClimbJumpingState()
    {
        
    }

    private void NoneToJumping()
    {
        currentClimbState = ClimbState.Jumping;
    }

    private void JumpingToNone()
    {
        currentClimbState = ClimbState.None;
    }

    private void JumpingToClimbing()
    {
        currentClimbState = ClimbState.Climbing;
        animator.SetBool("isClimbing", true);

        playerController.OnClimbStart();
    }

    private void ClimbingToJumping()
    {
        currentClimbState = ClimbState.Jumping;

        animator.SetBool("isClimbing", false);
        playerController.OnClimbEnd();
    }

    private void ClimbingToClimbJumping()
    {
        currentClimbState = ClimbState.ClimbJumping;
        animator.SetTrigger("climbing_jump");
        //施加一个远离墙壁和向上的力
        Vector2 jumpDir = Vector2.zero;
        if (isTouchingLeftWall)
            jumpDir = Vector2.right;
        else if (isTouchingRightWall)
            jumpDir = Vector2.left;

        float jumpFactor = 1f;
        //如果输入方向与墙壁相反，则增加跳跃水平力度
        if ((isTouchingLeftWall && inputX > 0.1f) || (isTouchingRightWall && inputX < -0.1f))
        {
            jumpFactor = 1.5f;
        }
        rb.velocity = new Vector2(jumpDir.x * ClimbJumpForce.x * jumpFactor, ClimbJumpForce.y);

        //一段时间后，状态切换回Jumping
        Invoke("ClimbJumpingToJumping", 0.2f);
    }

    private void ClimbJumpingToJumping()
    {
        currentClimbState = ClimbState.Jumping;
        animator.SetBool("isClimbing", false);
        playerController.OnClimbEnd();
    }

}
