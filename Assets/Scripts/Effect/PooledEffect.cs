using System.Collections;
using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    private EffectPoolManager owner;
    private ParticleSystem[] particleSystems;
    private TrailRenderer[] trailRenderers;
    private Animator[] animators;
    private Coroutine autoReturnCoroutine;
    private bool isReturningToPool;

    public int PoolKey { get; private set; }
    public bool IsManagedByPool => owner != null;

    public void Bind(EffectPoolManager poolOwner, int poolKey)
    {
        owner = poolOwner;
        PoolKey = poolKey;
        CacheComponents();
    }

    public void NotifyAfterSpawn()
    {
        CacheComponents();
        StopAutoReturnCoroutine();
        ClearTrailRenderers();
        RebindAnimators();
        RestartParticleSystems();
        autoReturnCoroutine = StartCoroutine(AutoReturnWhenFinished());
    }

    public void NotifyBeforeReturn()
    {
        StopAutoReturnCoroutine();
        StopParticleSystems();
    }

    public void ReturnToPool()
    {
        if (owner != null)
        {
            owner.ReturnToPool(this);
            return;
        }

        Destroy(gameObject);
    }

    public void OnAnimEnd()
    {
        ReturnToPool();
    }

    public void SetReturningToPool(bool returning)
    {
        isReturningToPool = returning;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying || isReturningToPool)
        {
            return;
        }

        StopAutoReturnCoroutine();
    }

    private IEnumerator AutoReturnWhenFinished()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            yield break;
        }

        while (true)
        {
            bool anyAlive = false;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem != null && particleSystem.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                break;
            }

            yield return null;
        }

        autoReturnCoroutine = null;
        ReturnToPool();
    }

    private void CacheComponents()
    {
        if (particleSystems == null)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        if (trailRenderers == null)
        {
            trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        }

        if (animators == null)
        {
            animators = GetComponentsInChildren<Animator>(true);
        }
    }

    private void RestartParticleSystems()
    {
        if (particleSystems == null)
        {
            return;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void StopParticleSystems()
    {
        if (particleSystems == null)
        {
            return;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void ClearTrailRenderers()
    {
        if (trailRenderers == null)
        {
            return;
        }

        for (int i = 0; i < trailRenderers.Length; i++)
        {
            TrailRenderer trailRenderer = trailRenderers[i];
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }
    }

    private void RebindAnimators()
    {
        if (animators == null)
        {
            return;
        }

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
            {
                continue;
            }

            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void StopAutoReturnCoroutine()
    {
        if (autoReturnCoroutine != null)
        {
            StopCoroutine(autoReturnCoroutine);
            autoReturnCoroutine = null;
        }
    }
}