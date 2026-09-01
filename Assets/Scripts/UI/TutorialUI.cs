using System.Collections;
using UnityEngine;
using LuaInterface;

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
    private int[] hideTokens;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CacheTutorialItems();
        HideAllTutorials();
    }

    private void CacheTutorialItems()
    {
        tutorialUIs = new GameObject[transform.childCount];
        hideTokens = new int[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            tutorialUIs[i] = transform.GetChild(i).gameObject;
        }
    }

    public void ShowTutorial(TutorialUITyepe type, float displayTime = 5f)
    {
        if (!IsValidType(type))
        {
            return;
        }

        hideTokens[(int)type]++;
        int currentToken = hideTokens[(int)type];

        if (!TryCallLuaShow(type, displayTime))
        {
            ShowTutorialFallback(type);
        }

        if (displayTime > 0f)
        {
            StartCoroutine(HideTutorialAfterTime(type, displayTime, currentToken));
        }
    }

    public void HideTutorial(TutorialUITyepe type)
    {
        if (!IsValidType(type))
        {
            return;
        }

        hideTokens[(int)type]++;

        if (TryCallLuaHide(type))
        {
            return;
        }

        HideTutorialFallback(type);
    }

    private bool TryCallLuaShow(TutorialUITyepe type, float displayTime)
    {
        LuaState luaState = GetLuaState();
        if (luaState == null)
        {
            return false;
        }

        LuaFunction function = luaState.GetFunction("TutorialPanelBridge.ShowTutorial", false);
        if (function == null)
        {
            return false;
        }

        function.Call(gameObject, (int)type, displayTime);
        function.Dispose();
        return true;
    }

    private bool TryCallLuaHide(TutorialUITyepe type)
    {
        LuaState luaState = GetLuaState();
        if (luaState == null)
        {
            return false;
        }

        LuaFunction function = luaState.GetFunction("TutorialPanelBridge.HideTutorial", false);
        if (function == null)
        {
            return false;
        }

        function.Call(gameObject, (int)type);
        function.Dispose();
        return true;
    }

    private LuaState GetLuaState()
    {
        return LuaClient.Instance != null ? LuaClient.GetMainState() : null;
    }

    private void ShowTutorialFallback(TutorialUITyepe type)
    {
        if (!IsValidType(type))
        {
            return;
        }

        GameObject tutorial = tutorialUIs[(int)type];
        if (tutorial.activeSelf)
        {
            return;
        }

        HideAllTutorials();
        tutorial.SetActive(true);
    }

    private IEnumerator HideTutorialAfterTime(TutorialUITyepe type, float time, int token)
    {
        yield return new WaitForSeconds(time);

        if (!IsValidType(type) || hideTokens[(int)type] != token)
        {
            yield break;
        }

        HideTutorial(type);
    }

    private void HideTutorialFallback(TutorialUITyepe type)
    {
        if (!IsValidType(type))
        {
            return;
        }

        tutorialUIs[(int)type].SetActive(false);
    }

    private void HideAllTutorials()
    {
        if (tutorialUIs == null)
        {
            return;
        }

        for (int i = 0; i < tutorialUIs.Length; i++)
        {
            if (tutorialUIs[i] != null)
            {
                tutorialUIs[i].SetActive(false);
            }
        }
    }

    private bool IsValidType(TutorialUITyepe type)
    {
        return tutorialUIs != null && type >= 0 && (int)type < tutorialUIs.Length;
    }
}
