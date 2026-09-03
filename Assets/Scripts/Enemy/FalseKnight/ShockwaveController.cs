using UnityEngine;

public class ShockwaveController : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private float direction;
    private float elapsedTime;
    private bool isActiveProjectile;
    private PooledProjectile pooledProjectile;

    [Header("生长设置")]
    [SerializeField] private float growDuration = 0.2f;
    private Vector3 targetScale;

    private void Awake()
    {
        ResolvePooledProjectile();
        targetScale = transform.localScale;
    }

    public void Initialize(float shockwaveSpeed, float shockwaveLifetime, float moveDirection)
    {
        ResolvePooledProjectile();
        speed = shockwaveSpeed;
        lifetime = shockwaveLifetime;
        direction = moveDirection;
        elapsedTime = 0f;
        isActiveProjectile = true;

        targetScale = new Vector3(Mathf.Abs(targetScale.x) * Mathf.Sign(moveDirection), targetScale.y, targetScale.z);
        transform.localScale = new Vector3(0f, targetScale.y, targetScale.z);
    }

    private void Update()
    {
        if (!isActiveProjectile)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);

        if (elapsedTime < growDuration)
        {
            float t = elapsedTime / Mathf.Max(growDuration, 0.0001f);
            float newX = Mathf.Lerp(0f, targetScale.x, t);
            transform.localScale = new Vector3(newX, targetScale.y, targetScale.z);
        }
        else
        {
            transform.localScale = targetScale;
        }

        if (elapsedTime >= lifetime)
        {
            Despawn();
        }
    }

    public void OnSpawnFromPool()
    {
        ResolvePooledProjectile();
        elapsedTime = 0f;
        isActiveProjectile = false;
        transform.localScale = targetScale;
    }

    public void OnDespawnToPool()
    {
        elapsedTime = 0f;
        isActiveProjectile = false;
        transform.localScale = targetScale;
    }

    private void Despawn()
    {
        isActiveProjectile = false;
        elapsedTime = 0f;
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