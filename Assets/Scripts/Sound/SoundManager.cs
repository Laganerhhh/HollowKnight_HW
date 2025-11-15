using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(string soundName, float volume = 1.0f)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audios/{soundName}");
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning($"Sound {soundName} not found!");
        }
    }

    public void PlayBGM(string bgmName, float volume = 1.0f)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audios/{bgmName}");
        if (clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM {bgmName} not found!");
        }
    }

    public void StopBGM()
    {
        audioSource.Stop();
    }
}
