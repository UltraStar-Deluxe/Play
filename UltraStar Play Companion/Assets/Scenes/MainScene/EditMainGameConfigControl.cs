using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditMainGameConfigControl : INeedInjection, IInjectionFinishedListener
{
    private const float ClickTimeThresholdInSeconds = 0.3f;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject(UxmlName = R.UxmlNames.volumeSlider)]
    private SliderInt volumeSlider;

    private bool isVolumeSliderInitialized;
    
    public void OnInjectionFinished()
    {
        mainGameHttpClient.ConnectionEventStream.Subscribe(_ => InitVolumeSlider());
    }
    
    private void InitVolumeSlider()
    {
        mainGameHttpClient.GetRequest("api/rest/config",
            response =>
            {
                if (isVolumeSliderInitialized)
                {
                    return;
                }

                MainGameSettingsDto mainGameSettingsDto = JsonConverter.FromJson<MainGameSettingsDto>(response, false);

                if (mainGameSettingsDto == null
                    || mainGameSettingsDto.AudioSettings == null)
                {
                    Debug.LogError($"Failed to get main game settings. Response: {response}");
                    return;
                }
                
                isVolumeSliderInitialized = true;
                volumeSlider.value = mainGameSettingsDto.AudioSettings.VolumePercent;

                // Only send a request when the new volume is stable. Therefor, use throttle of observable.
                Subject<int> volumeSliderValueChangedEventStream = new();
                volumeSlider.RegisterValueChangedCallback(evt => volumeSliderValueChangedEventStream.OnNext(evt.newValue));
                volumeSliderValueChangedEventStream
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .Subscribe(newValue => SetMainGameVolume(newValue));
            });
    }

    private void SetMainGameVolume(int newValue)
    {
        MainGameSettingsDto mainGameSettingsDto = new();
        mainGameSettingsDto.AudioSettings.VolumePercent = newValue;
        string postData = JsonConverter.ToJson(mainGameSettingsDto);
        mainGameHttpClient.PostRequest($"api/rest/config", postData);
    }
    
    public class MainGameSettingsDto
    {
        public MainGameAudioSettings AudioSettings = new();
    }

    public class MainGameAudioSettings
    {
        public int VolumePercent;
    }
}
