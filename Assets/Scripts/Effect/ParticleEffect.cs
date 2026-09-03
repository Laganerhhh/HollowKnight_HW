using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    private new ParticleSystem particleSystem;
    private PooledEffect pooledEffect;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        ResolvePooledEffect();
        if (particleSystem != null)
        {
            StartCoroutine(CheckAndRecycle());
        }
    }

    IEnumerator CheckAndRecycle()
    {
        // 等待粒子系统播放完毕
        while (particleSystem.isPlaying)
        {
            yield return null;
        }
        // 播放完毕后优先回池，未接入对象池时继续销毁
        ResolvePooledEffect();
        if (pooledEffect != null)
        {
            pooledEffect.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ResolvePooledEffect()
    {
        if (pooledEffect == null)
        {
            pooledEffect = GetComponent<PooledEffect>();
        }
    }
}
