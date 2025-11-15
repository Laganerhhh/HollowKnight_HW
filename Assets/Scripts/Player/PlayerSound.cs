using System.Collections;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    [Header("音效设置")]
    public AudioClip footstepClip;
    public AudioClip fallingClip;
    private AudioSource audioSource;
    private bool isWalking = false;
    private bool isFalling = false;
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
    }

    private void PlayFootstepSound()
    {
        if (audioSource == null || footstepClip == null)
            return;

        audioSource.clip = footstepClip;
        audioSource.Play();
    }
    
    private void StopFootstepSound()
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }

    public void SetWalkingState(bool walking)
    {
        bool wasWalking = isWalking;
        isWalking = walking;

        if (wasWalking && !walking)
        {
            //停止走路
            StopFootstepSound();
        }
        else if (!wasWalking && walking)
        {
            //开始走路时播放音效
            PlayFootstepSound();
        }
    }

    public void SetFallingState(bool falling)
    {
        bool wasFalling = isFalling;
        isFalling = falling;

        if (wasFalling && !falling)
        {
            //停止下落
            StopFallingSound();
        }
        else if (!wasFalling && falling)
        {
            //开始下落时播放音效
            PlayFallingSound();
        }
    }

    private void StopFallingSound()
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }
    
    private void PlayFallingSound()
    {
        if (audioSource == null || fallingClip == null)
            return;

        audioSource.clip = fallingClip;
        audioSource.Play();
    }
}