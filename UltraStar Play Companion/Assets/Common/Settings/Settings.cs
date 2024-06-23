using System;
using System.Collections.Generic;
using LiteNetLib;

[Serializable]
public class Settings : ISettings
{
    /**
     * UUID that is generated on first start and identifies this device.
     */
    public string ClientId { get; private set; }

    public string ClientName { get; set; } = "MyCompanionApp";

    public string CultureInfoName { get; set; } = "en";
    public MicProfile MicProfile { get; set; } = new MicProfile();
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; }
    public int TargetFps { get; set; } = 60;
    public bool ShowAudioWaveForm { get; set; } = true;
    public float MousePadSensitivity { get; set; } = 1f;
    public bool IsDevModeEnabled { get; set; }
    public ELogEventLevel MinimumLogLevel { get; set; } = ELogEventLevel.Information;

    public bool PlayRecordedAudio { get; set; }
    public int MicrophonePlaybackVolumePercent { get; set; } = 100;
    public PortAudioHostApi PortAudioHostApi { get; set; } = PortAudioHostApi.Default;
    public string PortAudioOutputDeviceName { get; set; } = "";

    // The settings use a list of deselected player such that players are selected by default.
    public List<string> DeselectedPlayerProfiles { get; private set; } = new();
    public Dictionary<string, MicProfileReference> PlayerProfileNameToLastUsedMicProfile { get; private set; } = new();

    public int ConnectionServerPort { get; set; } = 34567;
    public string ConnectionServerAddress { get; set; } = "";
    public DeliveryMethod MicDataDeliveryMethod { get; set; } = DeliveryMethod.ReliableOrdered;

    public GameRoundSettingsDto GameRoundSettingsDto { get; set; } = new();

    public void CreateAndSetClientId()
    {
        ClientId = Guid.NewGuid().ToString();
    }

    public void SetMicProfileName(string deviceName)
    {
        MicProfile newMicProfile = new MicProfile(MicProfile);
        newMicProfile.Name = deviceName;
        MicProfile = newMicProfile;
    }

    public bool ShowFps
    {
        get => IsDevModeEnabled;
        set => IsDevModeEnabled = value;
    }
}
