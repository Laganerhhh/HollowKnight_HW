using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    private new ParticleSystem particleSystem;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
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
        // 播放完毕后销毁游戏对象以回收资源
        Destroy(gameObject);
    }

}
