using UnityEngine;

[CreateAssetMenu(fileName = "NewLayerPreset", menuName = "空洞骑士工具/图层预设")]
public class LayerPreset : ScriptableObject
{
    public string presetName;
    public string sortingLayer = "Default";
    public int orderInLayer = 0;
    public Color tintColor = Color.white;
    public Material material;
    public float darkness = 0.3f;
    public bool hasCollider = false;
    public bool isParallax = false;
    public float parallaxFactor = 0.5f;
    public string tag = "Untagged";
}