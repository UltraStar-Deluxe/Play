
using System;
using UnityEngine;

[Serializable]
public class MicProfile
{
    public static readonly Comparison<MicProfile> compareByName =
        (micProfile1, micProfile2) => string.Compare(micProfile1.GetDisplayNameWithChannel(), micProfile2.GetDisplayNameWithChannel(), StringComparison.InvariantCulture);

    public string Name { get; set; }
    public int ChannelIndex { get; set; }
    public Color32 Color { get; set; } = Colors.crimson;
    public int Amplification { get; set; }
    public int NoiseSuppression { get; set; } = 5;
    public bool IsEnabled { get; set; } = true;
    public int DelayInMillis { get; set; } = 200;
    public int SampleRate { get; set; }

    // A connected companion app can be used as a mic. This string identifies the client.
    public string ConnectedClientId { get; set; }
    public bool IsInputFromConnectedClient => !ConnectedClientId.IsNullOrEmpty();

    public int AmplificationMultiplier => Convert.ToInt32(Math.Pow(10d, Amplification / 20d));

    public MicProfile()
    {
    }

    public MicProfile(string name, int channelIndex = 0, string connectedClientId = null)
    {
        this.Name = name;
        this.ChannelIndex = channelIndex;
        this.ConnectedClientId = connectedClientId;
    }

    public MicProfile(MicProfile other)
    {
        Name = other.Name;
        ChannelIndex = other.ChannelIndex;
        Color = other.Color;
        Amplification = other.Amplification;
        NoiseSuppression = other.NoiseSuppression;
        IsEnabled = other.IsEnabled;
        DelayInMillis = other.DelayInMillis;
        SampleRate = other.SampleRate;
        ConnectedClientId = other.ConnectedClientId;
    }

    protected bool Equals(MicProfile other)
    {
        return (IsInputFromConnectedClient
                && other.IsInputFromConnectedClient
                && ConnectedClientId == other.ConnectedClientId)
               || (!IsInputFromConnectedClient
                   && !other.IsInputFromConnectedClient
                   && Name == other.Name
                   && ChannelIndex == other.ChannelIndex);
    }

    protected bool Equals(MicProfileReference other)
    {
        return (IsInputFromConnectedClient
                && other.IsInputFromConnectedClient
                && ConnectedClientId == other.ConnectedClientId)
               || (!IsInputFromConnectedClient
                   && !other.IsInputFromConnectedClient
                   && Name == other.Name
                   && ChannelIndex == other.ChannelIndex);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is MicProfileReference otherMicProfileReference)
        {
            return Equals(otherMicProfileReference);
        }

        if (obj is MicProfile otherMicProfile)
        {
            return Equals(otherMicProfile);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, ChannelIndex, ConnectedClientId);
    }

    public override string ToString()
    {
        return this.GetDisplayNameWithChannel();
    }
}
