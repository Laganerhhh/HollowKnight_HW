using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class HotUpdateTest : MonoBehaviour
{
    public RawImage image;
    // Start is called before the first frame update
    void Start()
    {
        Addressables.LoadAssetAsync<Texture2D>("Assets/Textures/HollowKnightIcon.jpg").Completed +=
        (obj) =>
        {
            Texture2D tex = obj.Result;
            image.texture = tex;
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(tex.width, tex.height);
        };
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
