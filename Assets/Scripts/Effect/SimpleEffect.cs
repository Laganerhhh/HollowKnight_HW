using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEffect : MonoBehaviour
{
    private void OnAnimEnd()
    {
        Destroy(this.gameObject);
    }
}
