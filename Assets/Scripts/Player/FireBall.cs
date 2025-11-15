using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;
    public int damage = 1;

    private int dir = 1;

    public void Initialize(bool isRight)
    {
        if (isRight)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        dir = isRight ? 1 : -1;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(speed * Time.deltaTime * dir, 0, 0, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        //TODO
    }
}
