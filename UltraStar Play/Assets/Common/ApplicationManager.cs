using System;
using System.Collections.Generic;
using System.IO;
using PortAudioForUnity;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplicationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ApplicationManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ApplicationManager>();

    public List<string> simulatedCommandLineArguments = new();

    [Range(-1, 60)]
    public int targetFrameRate = 30;

    [Inject]
    private Settings settings;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    private int lastTargetFrameRate;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        targetFrameRate = settings.TargetFps;
        lastTargetFrameRate = targetFrameRate;
        ApplyTargetFrameRateAndVSync();

        settings.ObserveEveryValueChanged(it => it.TargetFps)
            .Subscribe(newValue => targetFrameRate = newValue)
            .AddTo(gameObject);

        ApplicationUtils.SetUsePortAudio(settings.PreferPortAudio);

        micSampleRecorderManager.ConnectedMicDevicesChangesStream
            .Subscribe(_ => OnConnectedMicDevicesChanged())
            .AddTo(gameObject);
    }

    private void OnConnectedMicDevicesChanged()
    {
        if (IMicrophoneAdapter.Instance.UsePortAudio)
        {
            Debug.LogWarning("Connected mic devices changed, but PortAudio is used for multi-channel support. A restart is required to use changed devices.");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_changedMicsRequireRestart));
        }
    }

    private void ApplyTargetFrameRateAndVSync()
    {
        if (targetFrameRate <= 0)
        {
            // Use the frame rate of the monitor
            Debug.Log("Set target frame rate to -1 (monitor refresh rate, vsync on)");
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            // Use the target frame rate
            Debug.Log($"Set target frame rate to {targetFrameRate} (vsync on)");
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
        }
    }

    private void Update()
    {
        if (this != Instance)
        {
            return;
        }

        if (lastTargetFrameRate != targetFrameRate)
        {
            lastTargetFrameRate = targetFrameRate;
            ApplyTargetFrameRateAndVSync();
        }
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
}
