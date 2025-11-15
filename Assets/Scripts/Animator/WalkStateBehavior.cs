using UnityEngine;

public class WalkStateBehavior : StateMachineBehaviour
{
    [Header("音效控制")]
    public bool enableFootsteps = true;
    public bool stopSoundOnExit = true;
    
    private PlayerSound playerSound;
    private bool hasInitialized = false;

    // 初始化playerSound
    private void Initialize(Animator animator)
    {
        if (hasInitialized) return;
        
        playerSound = animator.GetComponent<PlayerSound>();
        if (playerSound == null)
        {
            Debug.LogWarning("playerSound未在 " + animator.gameObject.name + " 上找到！");
        }
        hasInitialized = true;
    }

    // 进入状态时调用
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Initialize(animator);
        
        if (playerSound != null && enableFootsteps)
        {
            playerSound.SetWalkingState(true);
        }
    }
    
    // 退出状态时调用
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playerSound != null && stopSoundOnExit)
        {
            playerSound.SetWalkingState(false);
        }
    }
    
    // 当IK（反向动力学）更新时调用，确保在状态完全激活后执行
    override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!hasInitialized)
        {
            Initialize(animator);
        }
    }
}