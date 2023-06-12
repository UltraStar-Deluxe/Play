using System;

public class MicProfileReference : IEquatable<MicProfileReference>
{
    private readonly string name;
    public string Name => name;
    
    private readonly int channelIndex;
    public int ChannelIndex => channelIndex;

    public MicProfileReference(string name, int channelIndex)
    {
        this.name = name;
        this.channelIndex = channelIndex;
    }
    
    public MicProfileReference(MicProfile micProfile)
        : this(micProfile.Name, micProfile.ChannelIndex)
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

        return name == other.name && channelIndex == other.channelIndex;
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

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((MicProfileReference)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(name, channelIndex);
    }
}
