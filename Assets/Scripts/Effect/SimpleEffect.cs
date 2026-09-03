using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEffect : MonoBehaviour
{
    private PooledEffect pooledEffect;

    private void Awake()
    {
        ResolvePooledEffect();
    }

    private void OnAnimEnd()
    {
        ResolvePooledEffect();
        if (pooledEffect != null)
        {
            pooledEffect.ReturnToPool();
            return;
        }

        Destroy(this.gameObject);
    }

    private void ResolvePooledEffect()
    {
        if (pooledEffect == null)
        {
            pooledEffect = GetComponent<PooledEffect>();
        }
    }
}
