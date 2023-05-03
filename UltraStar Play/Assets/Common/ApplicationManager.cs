using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplicationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ApplicationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ApplicationManager>();

    public List<string> simulatedCommandLineArguments = new();

    [Range(-1, 60)]
    public int targetFrameRate = 30;

    [Inject]
    private Settings settings;
    
    private int lastScreenWidth;
    private int lastScreenHeight;
    private readonly Subject<Resolution> screenSizeChangedEventStream = new();
    public IObservable<Resolution> ScreenSizeChangedEventStream => screenSizeChangedEventStream;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        targetFrameRate = settings.targetFps;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        settings.ObserveEveryValueChanged(it => it.targetFps)
            .Subscribe(newValue => targetFrameRate = newValue);
            
        ApplicationUtils.SetUsePortAudio(settings.PreferPortAudio);
    }

    private void Update()
    {
        if (this != Instance)
        {
            return;
        }
        
        if (Application.targetFrameRate != targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
        }
        
        if (lastScreenHeight != Screen.height
            || lastScreenWidth != Screen.width)
        {
            screenSizeChangedEventStream.OnNext(Screen.currentResolution);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }

    protected override void OnEnableSingleton()
    {
        Application.logMessageReceivedThreaded += Log.HandleUnityLog;
    }

    protected override void OnDisableSingleton()
    {
        Application.logMessageReceivedThreaded -= Log.HandleUnityLog;
    }

    public bool HasCommandLineArgument(string argumentName)
    {
        string[] args = GetCommandLineArguments();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public string GetCommandLineArgument(string argumentName)
    {
        string[] args = GetCommandLineArguments();
        for (int i = 0; i < (args.Length - 1); i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.InvariantCultureIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return "";
    }

    public string[] GetCommandLineArguments()
    {
        if (Application.isEditor)
        {
            return simulatedCommandLineArguments.ToArray();
        }
        else
        {
            if (PlatformUtils.IsStandalone)
            {
                return Environment.GetCommandLineArgs();
            }
            else
            {
                return Array.Empty<string>();
            }
        }
    }

    public static string PersistentTempPath()
    {
        string path = Path.Combine(Application.persistentDataPath, "Temp");
        //Create Directory if it does not exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    public static string PersistentSongsPath()
    {
        string path = Path.Combine(Application.persistentDataPath, "Songs");
        //Create Directory if it does not exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
}
