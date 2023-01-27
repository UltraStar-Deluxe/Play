using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VolumeControl : AbstractSingletonBehaviour, INeedInjection
{
    public static VolumeControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<VolumeControl>();

    [Inject]
    private Settings settings;

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
}
