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

    private ClimbState currentClimbState = ClimbState.None;

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
        
    }

    private void HandleJumpingState()
    {
        
    }

    private void HandleClimbingState()
    {
        
    }

    private void HandleClimbJumpingState()
    {
        
    }

    private void NoneToJumping()
    {
        
    }

    private void JumpingToNone()
    {
        
    }

    private void JumpingToClimbing()
    {
        
    }

    private void ClimbingToJumping()
    {
        
    }

    private void ClimbingToClimbJumping()
    {
        
    }

    private void ClimbJumpingToJumping()
    {
        
    }

}
