using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class Settings
{
    public string ClientName { get; set; } = "MyCompanionApp";
    /**
     * UUID that is generated on first start and identifies this device.
     */
    public string ClientId { get; private set; }

    public MicProfile MicProfile { get; set; } = new MicProfile();

    public int TargetFps { get; set; } = 30;
    public bool ShowFps { get; set; }
    public bool ShowAudioWaveForm { get; set; } = true;

    public GameSettings GameSettings { get; set; } = new GameSettings();
    public AudioSettings AudioSettings { get; set; } = new AudioSettings();

    public bool IsDevModeEnabled { get; set; }

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
}
