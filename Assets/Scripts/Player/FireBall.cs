using System.Collections;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;
    public int damage = 1;

    private int dir = 1;
    private float remainingLifetime;
    private bool isActiveProjectile;
    private PooledProjectile pooledProjectile;

    private void Awake()
    {
        ResolvePooledProjectile();
    }

    public void Initialize(bool isRight)
    {
        ResolvePooledProjectile();

        if (isRight)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        dir = isRight ? 1 : -1;
        remainingLifetime = lifeTime;
        isActiveProjectile = true;
    }

    private void Update()
    {
        if (!isActiveProjectile)
        {
            return;
        }

        transform.Translate(speed * Time.deltaTime * dir, 0f, 0f, Space.Self);
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActiveProjectile || collision == null || !collision.CompareTag("Enermy"))
        {
            return;
        }

        EnermyHealth enermyHealth = collision.GetComponent<EnermyHealth>();
        if (enermyHealth != null)
        {
            enermyHealth.TakeDamage(damage);
        }

        Despawn();
    }

    public void OnSpawnFromPool()
    {
        ResolvePooledProjectile();
        remainingLifetime = lifeTime;
        isActiveProjectile = false;
    }

    public void OnDespawnToPool()
    {
        isActiveProjectile = false;
        remainingLifetime = lifeTime;
    }

    private void Despawn()
    {
        isActiveProjectile = false;
        remainingLifetime = lifeTime;
        if (pooledProjectile != null)
        {
            pooledProjectile.ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }

    private void ResolvePooledProjectile()
    {
        if (pooledProjectile == null)
        {
            pooledProjectile = GetComponent<PooledProjectile>();
        }
    }
}
