using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VolumeControl : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        volumeBeforeMute = -1;
    }
    private static int volumeBeforeMute = -1;
    
    public static VolumeControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<VolumeControl>();

    [Inject]
    private Settings settings;
    
    public bool IsMuted => volumeBeforeMute >= 0;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        UpdateGeneralVolume();
        settings.AudioSettings
            .ObserveEveryValueChanged(audioSettings => audioSettings.VolumePercent)
            .Subscribe(newValue => UpdateGeneralVolume())
            .AddTo(gameObject);
    }

    private void UpdateGeneralVolume()
    {
        AudioListener.volume = settings.AudioSettings.VolumePercent / 100.0f;
    }

    public void ToggleMuteAudio()
    {
        if (volumeBeforeMute >= 0)
        {
            settings.AudioSettings.VolumePercent = volumeBeforeMute;
            volumeBeforeMute = -1;
        }
        else
        {
            volumeBeforeMute = settings.AudioSettings.VolumePercent;
            settings.AudioSettings.VolumePercent = 0;
        }
    }
}
