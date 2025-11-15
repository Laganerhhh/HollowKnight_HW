using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private GameObject hitEffect;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Ground")
        {
            //剑气命中地面在击中点产生特效
            Instantiate(hitEffect, collision.ClosestPoint(transform.position), Quaternion.identity);
            SoundManager.instance.PlaySound(SoundIndex.player_hitRecoil);
        }
    }
}
