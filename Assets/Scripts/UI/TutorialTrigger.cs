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
            TutorialUI.instance.ShowTutorial(tutorialType, displayTime);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TutorialUI.instance.HideTutorial(tutorialType);
        }
    }
}
