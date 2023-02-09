using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditMainGameConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;
    
    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    private bool isControlsInitialized;
    
    public void OnInjectionFinished()
    {
        mainGameHttpClient.ConnectionEventStream.Subscribe(_ => InitControls());
    }
    
    private void InitControls()
    {
        mainGameHttpClient.GetRequest("api/rest/config",
            response =>
            {
                if (isControlsInitialized)
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
                
                isControlsInitialized = true;
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
