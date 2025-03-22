using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VolumeManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        volumeBeforeMute = -1;
    }
    private static int volumeBeforeMute = -1;

    public static VolumeManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<VolumeManager>();

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
        settings.ObserveEveryValueChanged(it => it.VolumePercent)
            .Subscribe(newValue => UpdateGeneralVolume())
            .AddTo(gameObject);
    }

    private void UpdateGeneralVolume()
    {
        AudioListener.volume = settings.VolumePercent / 100.0f;
    }

    public void ToggleMuteAudio()
    {
        if (volumeBeforeMute >= 0)
        {
            settings.VolumePercent = volumeBeforeMute;
            volumeBeforeMute = -1;
        }
        else
        {
            volumeBeforeMute = settings.VolumePercent;
            settings.VolumePercent = 0;
        }
    }

    private void OnApplicationQuit()
    {
        if (IsMuted)
        {
            Debug.Log("Unmuting in OnApplicationQuit");
            ToggleMuteAudio();
        }
    }
}
