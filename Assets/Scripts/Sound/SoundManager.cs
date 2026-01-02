using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    private AudioSource audioSource;

    public AudioClip defaultBGM;

    public string currentBGM = "";

    private void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (defaultBGM != null)
        {
            PlayBGM(defaultBGM);
        }
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

    public void PlayBGM(AudioClip audioClip, float volume = 1.0f)
    {
        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();
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

            currentBGM = bgmName;
        }
        else
        {
            Debug.LogWarning($"BGM {bgmName} not found!");
        }
    }

    public void SetBGMVolume(float volume)
    {
        audioSource.volume = volume;
    }

    public AudioClip GetAudioClip(string soundName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audios/{soundName}");
        if (clip == null)
        {
            Debug.LogWarning($"Sound {soundName} not found!");
        }
        return clip;
    }

    public void StopBGM()
    {
        audioSource.Stop();
    }
}
