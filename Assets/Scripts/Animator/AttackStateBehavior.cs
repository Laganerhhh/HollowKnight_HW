using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackStateBehavior : StateMachineBehaviour
{
    private bool hasInitialized = false;
    private PlayerController playerController;
    private void Initialize(Animator animator)
    {
        if (hasInitialized) return;

        playerController = animator.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController未在 " + animator.gameObject.name + " 上找到！");
        }
        hasInitialized = true;
    }
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!hasInitialized)
        {
            Initialize(animator);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playerController != null)
        {
            //如果当前状态的动画还未结束，则调用OnAttackEnd方法
            if (stateInfo.normalizedTime >= 0.9f)
            {
                playerController.OnAttackEnd();
            }
        }
    }
}
