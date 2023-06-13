using UnityEngine;

public interface ISettings
{
    public SystemLanguage Language { get; set; }
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; }

    public bool ShowFps { get; set; }
    
    public bool PlayRecordedAudio { get; set; }
    public int MicrophonePlaybackVolumePercent { get; set; }

    /**
     * The IP port on the server (e.g. Companion App) for initiating a connection.
     * Default value 34567.
     */
    public int IpPortOnServer { get; set; }

    /**
     * The IP address of the device running this app.
     * May be empty to select an IP address automatically.
     */
    public string OwnHost { get; set; }
}
