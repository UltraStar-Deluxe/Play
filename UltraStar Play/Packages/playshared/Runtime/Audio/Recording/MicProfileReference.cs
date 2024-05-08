using System;
using Newtonsoft.Json;

public class MicProfileReference : IEquatable<MicProfileReference>
{
    [JsonIgnore]
    private readonly string name;
    public string Name => name;

    [JsonIgnore]
    private readonly int channelIndex;
    public int ChannelIndex => channelIndex;

    [JsonIgnore]
    private readonly string connectedClientId;
    public string ConnectedClientId => connectedClientId;

    public bool IsInputFromConnectedClient => !ConnectedClientId.IsNullOrEmpty();

    public MicProfileReference()
    {
    }

    public MicProfileReference(string name, int channelIndex, string connectedClientId)
    {
        this.name = name;
        this.channelIndex = channelIndex;
        this.connectedClientId = connectedClientId;
    }

    public MicProfileReference(MicProfile micProfile)
        : this(micProfile.Name, micProfile.ChannelIndex, micProfile.ConnectedClientId)
    {
    }

    public bool Equals(MicProfileReference other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return (IsInputFromConnectedClient
                && other.IsInputFromConnectedClient
                && ConnectedClientId == other.ConnectedClientId)
               || (!IsInputFromConnectedClient
                   && !other.IsInputFromConnectedClient
                   && Name == other.Name
                   && ChannelIndex == other.ChannelIndex);
    }

    public bool Equals(MicProfile other)
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
        return HashCode.Combine(name, channelIndex);
    }
}
