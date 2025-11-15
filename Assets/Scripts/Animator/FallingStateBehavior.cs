using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingStateBehavior : StateMachineBehaviour
{
    private PlayerSound playerSound;
    private bool hasInitialized = false;

    private void Initialize(Animator animator)
    {
        if (hasInitialized) return;
        this.playerSound = animator.GetComponent<PlayerSound>();
        hasInitialized = true;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!hasInitialized) Initialize(animator);  
        playerSound.SetFallingState(true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!hasInitialized) Initialize(animator);  
        playerSound.SetFallingState(false);
    }
}
