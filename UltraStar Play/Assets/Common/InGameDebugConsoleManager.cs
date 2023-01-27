using IngameDebugConsole;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InGameDebugConsoleManager : AbstractSingletonBehaviour, INeedInjection
{
    public static InGameDebugConsoleManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<InGameDebugConsoleManager>();

    public bool hideDebugLogPopup;

    [Inject]
    private SceneNavigator sceneNavigator;

    private DebugLogManager debugLogManager;
    private DebugLogPopup debugLogPopup;
    private EventSystem debugLogEventSystem;

    private bool oldIsLogWindowVisible;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        debugLogManager = FindObjectOfType<DebugLogManager>(true);
        debugLogPopup = debugLogManager.GetComponentInChildren<DebugLogPopup>(true);
        debugLogEventSystem = debugLogManager.GetComponentInChildren<EventSystem>(true);

        UpdateDebugLogPopupVisible();
    }

    protected override void StartSingleton()
    {
        AddDebugLogConsoleCommands();

        sceneNavigator.SceneChangedEventStream.Subscribe(_ =>
        {
            // The EventSystem may be disabled afterwards because of EventSystemOptInOnAndroid. Thus, update after a frame.
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () =>
            {
                if (debugLogManager.IsLogWindowVisible)
                {
                    EnableInGameDebugConsoleEventSystemIfNeeded();
                }
            }));

            UpdateDebugLogPopupVisible();
        });
    }

    private void AddDebugLogConsoleCommands()
    {
        DebugLogConsole.AddCommand("logs.path", "Show path to log file",
            () => Debug.Log($"Log file path: {ApplicationUtils.GetLogFilePathDisplayString()}"));
    }

    private void Update()
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

    private void UpdateDebugLogPopupVisible()
    {
        if (hideDebugLogPopup
            || !Application.isEditor)
        {
            // The InGameDebugConsole has an NPE if the popup is disabled. Thus, only hide the UI components.
            debugLogPopup.GetComponentsInChildren<Image>().ForEach(image => image.enabled = false);
            debugLogPopup.GetComponentsInChildren<Text>().ForEach(text => text.enabled = false);
        }
    }

    private void EnableInGameDebugConsoleEventSystemIfNeeded()
    {
        InputSystemUIInputModule inputSystemUIInputModule = FindObjectOfType<InputSystemUIInputModule>(true);
        EventSystem inputSystemUIInputModuleEventSystem = inputSystemUIInputModule.GetComponentInChildren<EventSystem>(true);

        if (!inputSystemUIInputModuleEventSystem.gameObject.activeInHierarchy)
        {
            debugLogEventSystem.gameObject.SetActive(true);
        }
    }

    private void DisableInGameDebugConsoleEventSystem()
    {
        debugLogEventSystem.gameObject.SetActive(false);
    }

    public void ShowConsole()
    {
        debugLogManager.ShowLogWindow();
    }
}
