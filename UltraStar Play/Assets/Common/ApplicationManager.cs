using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
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

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        targetFrameRate = settings.GraphicSettings.targetFps;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Update()
    {
        if (Application.targetFrameRate != targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
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
                return System.Environment.GetCommandLineArgs();
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
