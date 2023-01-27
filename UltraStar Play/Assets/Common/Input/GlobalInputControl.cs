using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;

public class GlobalInputControl : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        volumeBeforeMute = -1;
    }

    private static int volumeBeforeMute = -1;

    [Inject]
    private Settings settings;

    protected void Start()
    {
        // Toggle full-screen mode via F11
        InputManager.GetInputAction(R.InputActions.usplay_toggleFullscreen).PerformedAsObservable()
            .Subscribe(_ => ToggleFullscreen());

        // Mute / unmute audio via F12
        InputManager.GetInputAction(R.InputActions.usplay_toggleMute).PerformedAsObservable()
            .Subscribe(_ => ToggleMuteAudio());
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

    public void ToggleMuteAudio()
    {
        if (volumeBeforeMute >= 0)
        {
            settings.AudioSettings.VolumePercent = volumeBeforeMute;
            volumeBeforeMute = -1;
            UiManager.CreateNotification("Unmute");
        }
        else
        {
            volumeBeforeMute = settings.AudioSettings.VolumePercent;
            settings.AudioSettings.VolumePercent = 0;
            UiManager.CreateNotification("Mute");
        }
    }
}
