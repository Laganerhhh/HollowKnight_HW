using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitEffect : MonoBehaviour
{
    private PooledEffect pooledEffect;

    private void Awake()
    {
        ResolvePooledEffect();
    }

    //动画播放完毕后销毁特效对象
    private void OnAnimEnd()
    {
        ResolvePooledEffect();
        if (pooledEffect != null)
        {
            pooledEffect.ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }

    private void ResolvePooledEffect()
    {
        if (pooledEffect == null)
        {
            pooledEffect = GetComponent<PooledEffect>();
        }
    }
}
