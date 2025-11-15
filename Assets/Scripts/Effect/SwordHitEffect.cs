using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitEffect : MonoBehaviour
{
    //动画播放完毕后销毁特效对象
    private void OnAnimEnd()
    {
        Destroy(gameObject);
    }
}
