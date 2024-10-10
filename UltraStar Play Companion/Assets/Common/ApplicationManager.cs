using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ApplicationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ApplicationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ApplicationManager>();

    public List<string> simulatedCommandLineArguments = new List<string>();

    [Range(-1, 60)]
    public int targetFrameRate = 30;

    [Inject]
    private Settings settings;

    private readonly Subject<Finger> fingerUpEventStream = new();
    public IObservable<Finger> FingerUpEventStream => fingerUpEventStream;

    private readonly Subject<Finger> fingerDownEventStream = new();
    public IObservable<Finger> FingerDownEventStream => fingerDownEventStream;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        EnhancedTouchSupport.Enable();
    }

    protected override void StartSingleton()
    {
        UpdateTargetFps();
        settings.ObserveEveryValueChanged(it => it.TargetFps)
            .Subscribe(_ => UpdateTargetFps())
            .AddTo(gameObject);
    }

    private void UpdateTargetFps()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        targetFrameRate = settings.TargetFps;
        if (targetFrameRate > 0)
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 1;
        }
    }

    protected override void OnEnableSingleton()
    {
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerUp += OnFingerUp;
    }

    protected override void OnDisableSingleton()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerUp -= OnFingerUp;
    }

    void Update()
    {
        if (Application.targetFrameRate != targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
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

    private void OnFingerUp(Finger obj)
    {
       fingerUpEventStream.OnNext(obj);
    }

    private void OnFingerDown(Finger obj)
    {
       fingerDownEventStream.OnNext(obj);
    }
}
