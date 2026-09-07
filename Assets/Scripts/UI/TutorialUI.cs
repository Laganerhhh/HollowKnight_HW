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

    private static readonly string[] TutorialNodeNames =
    {
        "tutorial_jump",
        "tutorial_attack",
        "tutorial_climb",
        "tutorial_superdash",
        "tutorial_recover",
        "tutorial_skill",
        "tutorial_dash",
        "tutorial_attack_down"
    };

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
        tutorialUIs = new GameObject[TutorialNodeNames.Length];
        hideTokens = new int[TutorialNodeNames.Length];

        for (int i = 0; i < TutorialNodeNames.Length; i++)
        {
            Transform child = transform.Find(TutorialNodeNames[i]);
            tutorialUIs[i] = child != null ? child.gameObject : null;

            if (tutorialUIs[i] == null)
            {
                Debug.LogWarning($"[TutorialUI] Missing tutorial node: {TutorialNodeNames[i]}", this);
            }
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
        if (!TryGetTutorialObject(type, out GameObject tutorial))
        {
            return;
        }

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
        if (!TryGetTutorialObject(type, out GameObject tutorial))
        {
            return;
        }

        tutorial.SetActive(false);
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
        return type >= 0 && (int)type < TutorialNodeNames.Length;
    }

    private bool TryGetTutorialObject(TutorialUITyepe type, out GameObject tutorial)
    {
        tutorial = null;
        if (!IsValidType(type))
        {
            return false;
        }

        if (tutorialUIs == null || tutorialUIs.Length != TutorialNodeNames.Length)
        {
            CacheTutorialItems();
        }

        tutorial = tutorialUIs[(int)type];
        if (tutorial == null)
        {
            CacheTutorialItems();
            tutorial = tutorialUIs[(int)type];
        }

        if (tutorial == null)
        {
            Debug.LogWarning($"[TutorialUI] Tutorial node is missing for type {type}.", this);
            return false;
        }

        return true;
    }
}
