using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public TutorialUITyepe tutorialType;

    public float displayTime = 20f;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (TutorialUI.instance != null)
            {
                TutorialUI.instance.ShowTutorial(tutorialType, displayTime);
            }
            else
            {
                Debug.LogWarning("[TutorialTrigger] TutorialUI.instance is null when entering trigger.", this);
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (TutorialUI.instance != null)
            {
                TutorialUI.instance.HideTutorial(tutorialType);
            }
            else
            {
                Debug.LogWarning("[TutorialTrigger] TutorialUI.instance is null when exiting trigger.", this);
            }
        }
    }
}
