using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Settings : ISettings
{
    /**
     * UUID that is generated on first start and identifies this device.
     */
    public string ClientId { get; private set; }
    
    public string ClientName { get; set; } = "MyCompanionApp";

    public SystemLanguage Language { get; set; } = SystemLanguage.English;
    public MicProfile MicProfile { get; set; } = new MicProfile();
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; }
    public int TargetFps { get; set; } = 60;
    public bool ShowAudioWaveForm { get; set; } = true;
    public float MousePadSensitivity { get; set; } = 1;
    public bool IsDevModeEnabled { get; set; }
    
    // The settings use a list of deselected player such that players are selected by default.
    public List<string> DeselectedPlayerProfiles { get; private set; } = new();

    public int UdpPortOnServer { get; set; } = 34567;
    public int UdpPortOnClient { get; set; } = 34568;
    public string OwnHost { get; set; }

    public GameRoundSettings GameRoundSettings { get; set; } = new();
    
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
