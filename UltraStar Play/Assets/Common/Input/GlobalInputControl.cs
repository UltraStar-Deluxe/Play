using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;

public class GlobalInputControl : AbstractSingletonBehaviour, INeedInjection
{
    public static GlobalInputControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<GlobalInputControl>();

    [Inject]
    private Settings settings;

    [Inject]
    private VolumeControl volumeControl;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        // Toggle full-screen mode via F11
        InputManager.GetInputAction(R.InputActions.usplay_toggleFullscreen).PerformedAsObservable()
            .Subscribe(_ => ToggleFullscreen());

        // Mute / unmute audio via F12
        InputManager.GetInputAction(R.InputActions.usplay_toggleMute).PerformedAsObservable()
            .Subscribe(_ => ToggleMuteAudio());
    }

    private void ToggleMuteAudio()
    {
        volumeControl.ToggleMuteAudio();
        if (volumeControl.IsMuted)
        {
            UiManager.CreateNotification("Mute");
        }
        else
        {
            UiManager.CreateNotification("Unmute");
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
                settings.GraphicSettings.fullScreenMode = Screen.fullScreenMode;
                Debug.Log("New full-screen mode " + settings.GraphicSettings.fullScreenMode);
            }));
    }
}
