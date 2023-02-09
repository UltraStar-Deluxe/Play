using IngameDebugConsole;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public abstract class AbstractInGameDebugConsoleManager : AbstractSingletonBehaviour
{
    public bool hideDebugLogPopup;

    protected DebugLogManager debugLogManager;
    protected DebugLogPopup debugLogPopup;
    protected EventSystem debugLogEventSystem;

    protected bool oldIsLogWindowVisible;

    protected virtual void Init()
    {
        debugLogManager = FindObjectOfType<DebugLogManager>(true);
        debugLogPopup = debugLogManager.GetComponentInChildren<DebugLogPopup>(true);
        debugLogEventSystem = debugLogManager.GetComponentInChildren<EventSystem>(true);

        UpdateDebugLogPopupVisible();

        AddDebugLogConsoleCommands();
    }

    protected virtual void AddDebugLogConsoleCommands()
    {
        DebugLogConsole.AddCommand("logs.path", "Show path to log file",
            () => Debug.Log($"Log file path: {ApplicationUtils.ReplacePathsWithDisplayString(Log.logFilePath)}"));
    }

    protected virtual void Update()
    {
        if (debugLogManager.IsLogWindowVisible
            && !oldIsLogWindowVisible)
        {
            EnableInGameDebugConsoleEventSystemIfNeeded();
            UpdateDebugLogPopupVisible();
        }
        else if (!debugLogManager.IsLogWindowVisible
                 && oldIsLogWindowVisible)
        {
            DisableInGameDebugConsoleEventSystem();
            UpdateDebugLogPopupVisible();
        }

        oldIsLogWindowVisible = debugLogManager.IsLogWindowVisible;
    }

    protected virtual void UpdateDebugLogPopupVisible()
    {
        if (hideDebugLogPopup
            || !Application.isEditor)
        {
            // The InGameDebugConsole has an NPE if the popup is disabled. Thus, only hide the UI components.
            debugLogPopup.GetComponentsInChildren<Image>().ForEach(image => image.enabled = false);
            debugLogPopup.GetComponentsInChildren<Text>().ForEach(text => text.enabled = false);
        }
    }

    protected virtual void EnableInGameDebugConsoleEventSystemIfNeeded()
    {
        InputSystemUIInputModule inputSystemUIInputModule = FindObjectOfType<InputSystemUIInputModule>(true);
        if (inputSystemUIInputModule == null)
        {
            debugLogEventSystem.gameObject.SetActive(true);
            return;
        }

        EventSystem inputSystemUIInputModuleEventSystem = inputSystemUIInputModule.GetComponentInChildren<EventSystem>(true);
        if (inputSystemUIInputModuleEventSystem == null
            || !inputSystemUIInputModuleEventSystem.gameObject.activeInHierarchy)
        {
            debugLogEventSystem.gameObject.SetActive(true);
        }
    }

    protected virtual void DisableInGameDebugConsoleEventSystem()
    {
        debugLogEventSystem.gameObject.SetActive(false);
    }

    public virtual void ShowConsole()
    {
        debugLogManager.ShowLogWindow();
    }
}
