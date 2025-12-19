using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 震动区域脚本
/// </summary>
public class ShakeRegion : MonoBehaviour
{
    public float shakeDuration = 0.5f;
    public float shakeIntensity = 1.0f;

    private bool canShake = true;

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player" && canShake)
        {
            CameraManager.instance.Shake(shakeIntensity, shakeDuration);
            canShake = false;
            StartCoroutine(ResetShakeCooldown());
        }
    }

    IEnumerator ResetShakeCooldown()
    {
        yield return new WaitForSeconds(shakeDuration);
        canShake = true;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            CameraManager.instance.StopShake();
        }
    }
}
