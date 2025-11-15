using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperDashStatement : StateMachineBehaviour
{
    private SuperDash superDash;

    private bool hasInitialized = false;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("superDash_sprint"))
        {
            if (!hasInitialized)
            {
                superDash = animator.GetComponent<SuperDash>();
            }
            superDash.ResetAll();
        }
    }
}
