using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Region : MonoBehaviour
{
    public enum RegionType
    {
        Indoor,
        Outdoor
    }

    public RegionType regionType = RegionType.Outdoor;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (regionType == RegionType.Indoor)
            {
                //进入室内区域，摄像机回复
                CameraManager.instance.SetIndoorCamera();
            }
            else
            {
                //进入室外区域，摄像机拉远
                CameraManager.instance.SetOutdoorCamera();
            }
            
        }
    }

}
