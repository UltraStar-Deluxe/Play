using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VolumeControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    private void Start()
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
