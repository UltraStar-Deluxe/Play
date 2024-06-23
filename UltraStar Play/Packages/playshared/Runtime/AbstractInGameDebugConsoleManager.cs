using System.IO;
using IngameDebugConsole;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using UniRx;
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

    private static readonly string serilogOutputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{StackTrace}";
    private ITextFormatter serilogTextFormatter = new MessageTemplateTextFormatter(serilogOutputTemplate);
    
    protected virtual void Init()
    {
        debugLogManager = FindObjectOfType<DebugLogManager>(true);
        debugLogPopup = debugLogManager.GetComponentInChildren<DebugLogPopup>(true);
        debugLogEventSystem = debugLogManager.GetComponentInChildren<EventSystem>(true);

        Log.GetLogHistory().ForEach(OnSerilogLogEvent);
        Log.LogEventStream.Subscribe(OnSerilogLogEvent);
        
        UpdateDebugLogPopupVisible();

        AddDebugLogConsoleCommands();
    }

    private void OnSerilogLogEvent(LogEvent logEvent)
    {
        if (Log.IsUsingDefaultUnityLogHandler)
        {
            // Already logging to the InGameDebugConsole via the default Unity ILogHandler implementation.
            return;
        }
        
        using StringWriter stringBuffer = new();
        serilogTextFormatter.Format(logEvent, stringBuffer);
        string logString = stringBuffer.ToString();
        
        debugLogManager.ReceivedLog(logString, logEvent.Exception?.StackTrace, Log.GetUnityLogType(logEvent));
    }
    
    protected virtual void AddDebugLogConsoleCommands()
    {
        AddDebugLogPathConsoleCommands();
    }

    private void AddDebugLogPathConsoleCommands()
    {
        DebugLogConsole.AddCommand("logs.path", "Show path to log file",
            () => Debug.Log($"Log file path: {ApplicationUtils.ReplacePathsWithDisplayString(Log.logFilePath)}"));

        DebugLogConsole.AddCommand("logs.path.copy", "Copy path to log file",
            () =>
            {
                string logFilePath = ApplicationUtils.ReplacePathsWithDisplayString(Log.logFilePath);
                ClipboardUtils.CopyToClipboard(logFilePath);
                Debug.Log($"Copied to clipboard: {logFilePath}");
            });

        if (PlatformUtils.IsStandalone)
        {
            DebugLogConsole.AddCommand("logs.path.open", "Open folder with log file",
                () =>
                {
                    string logFilePath = ApplicationUtils.ReplacePathsWithDisplayString(Log.logFilePath);
                    ApplicationUtils.OpenDirectory(new FileInfo(logFilePath).DirectoryName);
                    Debug.Log($"Open folder: {logFilePath}");
                });
        }
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
