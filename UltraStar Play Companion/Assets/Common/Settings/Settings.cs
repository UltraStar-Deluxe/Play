using System;

[Serializable]
public class Settings
{
    public string ClientName { get; set; } = "MyCompanionApp";
    /**
     * UUID that is generated on first start and identifies this device.
     */
    public string ClientId { get; private set; }
    public string RecordingDeviceName { get; set; }
    public int SampleRate { get; set; }
    public int TargetFps { get; set; } = 30;
    public bool ShowFps { get; set; }
    public bool ShowAudioWaveForm { get; set; } = true;

    public GameSettings GameSettings { get; set; } = new GameSettings();
    public AudioSettings AudioSettings { get; set; } = new AudioSettings();

    public void CreateAndSetClientId()
    {
        ClientId = Guid.NewGuid().ToString();
    }
}
