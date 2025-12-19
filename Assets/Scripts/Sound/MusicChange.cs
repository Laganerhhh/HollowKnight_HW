using System.Collections;
using System.Collections.Generic;
using UnityEngine;


enum MusicArea
{
    FirstGameArea,
    ColosseumLv1,
    FinalStage1,
    FinalStage2,
    FinalStage3,
    FinalStage4,
    FinalStage5
}

/// <summary>
/// 当进入当前区域，切换背景音乐
/// </summary>
public class MusicChange : MonoBehaviour
{
    [SerializeField] private MusicArea musicArea;

    [SerializeField] private float volume = 0.4f;

    [SerializeField] private float fadeDuration = 1.0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ChangeMusic();
        }
    }

    //获取音乐优先级，优先级高的可以被覆盖低优先级的音乐
    private int GetMusicOrder(string bgmName)
    {
        if (bgmName == SoundIndex.final_stage_bg_5)
            return 5;
        else if (bgmName == SoundIndex.final_stage_bg_4)
            return 4;
        else if (bgmName == SoundIndex.final_stage_bg_3)
            return 3;
        else if (bgmName == SoundIndex.final_stage_bg_2)
            return 2;
        else if (bgmName == SoundIndex.final_stage_bg_1)
            return 1;
        else
            return 0;
    }

    private IEnumerator FadeOutAndIn(float fadeDuration)
    {
        float startVolume = volume;
        float targetVolume = 0f;
        float elapsedTime = 0f;

        // Fade out
        while (elapsedTime < fadeDuration)
        {
            volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeDuration);
            SoundManager.instance.SetBGMVolume(volume); // 假设有一个设置音量的方法
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        volume = targetVolume;
        SoundManager.instance.SetBGMVolume(volume);

        // Play new music
        string newBGM = GetBGMNameByArea(musicArea);
        SoundManager.instance.PlayBGM(newBGM, volume);

        // Fade in
        elapsedTime = 0f;
        targetVolume = 0.4f; // 设定的新音量
        while (elapsedTime < fadeDuration)
        {
            volume = Mathf.Lerp(0f, targetVolume, elapsedTime / fadeDuration);
            SoundManager.instance.SetBGMVolume(volume);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        volume = targetVolume;
        SoundManager.instance.SetBGMVolume(volume);
    }

    private void ChangeMusic()
    {
        int currentOrder = GetMusicOrder(SoundManager.instance.currentBGM);
        int newOrder = GetMusicOrder(GetBGMNameByArea(musicArea));

        if (newOrder > currentOrder) //只有当新的音乐优先级高于当前音乐时才切换,且不重复播放当前音乐
        {
            StartCoroutine(FadeOutAndIn(fadeDuration)); //淡入淡出
        }
    }

    private string GetBGMNameByArea(MusicArea area)
    {
        switch (area)
        {
            case MusicArea.FirstGameArea:
                return SoundIndex.first_game_BG;
            case MusicArea.ColosseumLv1:
                return SoundIndex.colosseumLv1_BG;
            case MusicArea.FinalStage1:
                return SoundIndex.final_stage_bg_1;
            case MusicArea.FinalStage2:
                return SoundIndex.final_stage_bg_2;
            case MusicArea.FinalStage3:
                return SoundIndex.final_stage_bg_3;
            case MusicArea.FinalStage4:
                return SoundIndex.final_stage_bg_4;
            case MusicArea.FinalStage5:
                return SoundIndex.final_stage_bg_5;
            default:
                return "";
        }
    }
}
