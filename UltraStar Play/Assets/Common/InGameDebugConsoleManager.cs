using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InGameDebugConsoleManager : AbstractInGameDebugConsoleManager, INeedInjection
{
    public static InGameDebugConsoleManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<InGameDebugConsoleManager>();

    [Inject]
    private SceneNavigator sceneNavigator;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        base.Init();
    }

    protected override void StartSingleton()
    {
        sceneNavigator.SceneChangedEventStream.Subscribe(async _ =>
            {
                // The EventSystem may be disabled afterwards because of EventSystemOptInOnAndroid. Thus, update after a frame.
                await Awaitable.NextFrameAsync();
                if (debugLogManager.IsLogWindowVisible)
                {
                    EnableInGameDebugConsoleEventSystemIfNeeded();
                }

                UpdateDebugLogPopupVisible();
            })
            .AddTo(gameObject);
    }
}
