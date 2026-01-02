using UnityEngine;
using System.Collections;

public class HitFlash : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    
    [Header("材质引用")]
    public Material originalMaterial; // 拖入怪物平时的材质 (Sprites-Default)
    public Material flashMaterial;   // 拖入你新建的 GUI/Text 材质

    [Header("设置")]
    public float flashDuration = 0.1f; // 变白持续时间

    private Coroutine _flashRoutine;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 自动获取当前的材质作为初始材质，防止手动拖拽遗漏
        if (originalMaterial == null)
        {
            originalMaterial = _spriteRenderer.material;
        }
    }

    public void Flash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 1. 切换到发白材质
        _spriteRenderer.material = flashMaterial;

        // 2. 等待
        yield return new WaitForSeconds(flashDuration);

        // 3. 切换回原始材质
        _spriteRenderer.material = originalMaterial;

        _flashRoutine = null;
    }
}