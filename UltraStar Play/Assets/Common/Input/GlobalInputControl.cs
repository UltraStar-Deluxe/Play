using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class GlobalInputControl : AbstractSingletonBehaviour, INeedInjection
{
    public static GlobalInputControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<GlobalInputControl>();

    [Inject]
    private Settings settings;

    [Inject]
    private VolumeControl volumeControl;

    [Inject]
    private SceneRecipeManager sceneRecipeManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private UIDocument uiDocument;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        RegisterInputActions();
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => RegisterInputActions());
    }

    private void RegisterInputActions()
    {
        InputManager.GetInputAction(R.InputActions.usplay_toggleFullscreen).PerformedAsObservable()
            .Subscribe(_ => ToggleFullscreen());

        InputManager.GetInputAction(R.InputActions.usplay_toggleMute).PerformedAsObservable()
            .Subscribe(_ => ToggleMuteAudio());
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Application.isEditor
            && Keyboard.current.f4Key.wasReleasedThisFrame)
        {
            // Toggle UI visibility
            Debug.Log("Toggle UI visibility");
            uiDocument.rootVisualElement.SetVisibleByDisplay(!uiDocument.rootVisualElement.IsVisibleByDisplay());
        }
    }

    private void ToggleMuteAudio()
    {
        volumeControl.ToggleMuteAudio();
        if (volumeControl.IsMuted)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_mute));
        }
        else
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_unmute));
        }
    }

    private void ToggleFullscreen()
    {
        Debug.Log("Toggle full-screen mode");
        Screen.fullScreen = !Screen.fullScreen;
        // A full-screen switch does not happen immediately; it will actually happen when the current frame is finished.
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(2,
            () =>
            {
                settings.FullScreenMode = Screen.fullScreenMode.ToCustomFullScreenMode();
                Debug.Log("New full-screen mode " + settings.FullScreenMode);
            }));
    }
}
