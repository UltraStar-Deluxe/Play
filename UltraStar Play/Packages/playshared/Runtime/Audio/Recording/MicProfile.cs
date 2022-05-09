
using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class MicProfile
{
    public static readonly Comparison<MicProfile> compareByName =
        (micProfile1, micProfile2) => string.Compare(micProfile1.Name, micProfile2.Name, StringComparison.InvariantCulture);

    public string Name { get; set; }
    public Color32 Color { get; set; } = Colors.crimson;
    public int Amplification { get; set; }
    public int NoiseSuppression { get; set; } = 5;
    public bool IsEnabled { get; set; }
    public int DelayInMillis { get; set; } = 300;
    public int SampleRate { get; set; }

    // A connected companion app can be used as a mic. This string identifies the client.
    public string ConnectedClientId { get; set; }
    public bool IsInputFromConnectedClient => !ConnectedClientId.IsNullOrEmpty();

    public int AmplificationMultiplier => Convert.ToInt32(Math.Pow(10d, Amplification / 20d));

    public MicProfile()
    {
    }

    public MicProfile(string name, string connectedClientId = null)
    {
        this.Name = name;
        this.ConnectedClientId = connectedClientId;
    }

    public MicProfile(MicProfile other)
    {
        Name = other.Name;
        Color = other.Color;
        Amplification = other.Amplification;
        NoiseSuppression = other.NoiseSuppression;
        IsEnabled = other.IsEnabled;
        DelayInMillis = other.DelayInMillis;
        SampleRate = other.SampleRate;
        ConnectedClientId = other.ConnectedClientId;
    }
}
