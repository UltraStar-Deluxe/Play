using UnityEngine;

public interface ISettings
{
    public SystemLanguage Language { get; set; }
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; }

    public bool ShowFps { get; set; }
    
    public bool PlayRecordedAudio { get; set; }
    public int MicrophonePlaybackVolumePercent { get; set; }

    public PortAudioHostApi PortAudioHostApi { get; set; }
    public string PortAudioOutputDeviceName { get; set; }
}
