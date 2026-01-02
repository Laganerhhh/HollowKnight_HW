using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour, 
IPointerEnterHandler, // 鼠标悬停
IPointerExitHandler,  // 鼠标离开
IPointerDownHandler,  // 按下
IPointerUpHandler,    // 释放
ISelectHandler,       // 选择（键盘/手柄导航）
IDeselectHandler      // 取消选择
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 播放悬停音效
        SoundManager.instance.PlaySound(SoundIndex.ui_button_selected);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 可选：播放离开音效
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 播放按下音效
        SoundManager.instance.PlaySound(SoundIndex.ui_button_confirm);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 可选：播放释放音效
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 可选：播放选择音效
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // 可选：播放取消选择音效
    }

    private void OnEnable()
    {
        // 延迟一帧重置状态，确保组件完全初始化
        StartCoroutine(ResetButtonState());
    }

    private IEnumerator ResetButtonState()
    {
        yield return null; // 等待一帧
        
        // 强制切换到Normal状态
        if (TryGetComponent<Button>(out var button))
        {
            button.animator?.Play("Normal");
            // 或者重新启用交互
            button.interactable = false;
            button.interactable = true;
        }
    }
}
