using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("引用设置")]
    [SerializeField]
    [Tooltip("拖入子物体 fk-fireball")]
    private Animator ballAnimator; 

    [Header("基本属性")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float destroyDelay = 0.5f;
    [SerializeField] private GameObject tail_particle;
    [SerializeField] private GameObject explode_particle;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private bool _hasExploded = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();

        // 如果你忘记在面板拖拽，代码尝试自动在子物体中寻找
        if (ballAnimator == null)
        {
            ballAnimator = GetComponentInChildren<Animator>();
            // 或者明确指定名字：ballAnimator = transform.Find("fk-fireball").GetComponent<Animator>();
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasExploded) return;

        string layerName = LayerMask.LayerToName(collision.gameObject.layer);

        if (layerName == "Player")
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Explode();
        }
        else if (layerName == "Terrian")
        {
            tail_particle.SetActive(false);

            GameObject explode_part = Instantiate(explode_particle, transform.position, Quaternion.identity);
            explode_part.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Explode();
        }
    }

    private void Explode()
    {
        _hasExploded = true;

        // 1. 停止物理
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.simulated = false; 
        }

        if (_collider != null) _collider.enabled = false;

        // 2. 调用子物体的 Animator
        if (ballAnimator != null)
        {
            ballAnimator.SetTrigger("hit");
        }

        // 3. 销毁整个父物体（连同子物体一起销毁）
        Destroy(gameObject, destroyDelay);
    }
}