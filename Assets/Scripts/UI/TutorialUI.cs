using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialUITyepe
{
    Jump,
    Attack,
    Climb,
    SuperDash,
    Recover,
    Skill,
    Dash,
    Attack_Down
}

public class TutorialUI : MonoBehaviour
{
    public static TutorialUI instance;

    [SerializeField] private GameObject[] tutorialUIs;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        //找到所有子物体
        tutorialUIs = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            tutorialUIs[i] = transform.GetChild(i).gameObject;
            tutorialUIs[i].SetActive(false);
        }
    }

    public void ShowTutorial(TutorialUITyepe type, float displayTime = 5f)
    {
        if (type < 0 || type >= (TutorialUITyepe)tutorialUIs.Length)
            return;
        if (tutorialUIs[(int)type].activeSelf)
            return;
        //先将其它提示隐藏
        for (int i = 0; i < tutorialUIs.Length; i++)
        {
            tutorialUIs[i].SetActive(false);
        }
        tutorialUIs[(int)type].SetActive(true);
        StartCoroutine(HideTutorialAfterTime(type, displayTime));
    }

    private IEnumerator HideTutorialAfterTime(TutorialUITyepe type, float time)
    {
        yield return new WaitForSeconds(time);
        HideTutorial(type);
    }

    public void HideTutorial(TutorialUITyepe type)
    {
        if (type < 0 || type >= (TutorialUITyepe)tutorialUIs.Length)
            return;
        tutorialUIs[(int)type].SetActive(false);
    }

}
