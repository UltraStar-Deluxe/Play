public class MicProfileReference
{
    public string Name { get; private set; }
    public int ChannelIndex { get; private set; }

    public MicProfileReference(string name, int channelIndex)
    {
        Name = name;
        ChannelIndex = channelIndex;
    }
    
    public MicProfileReference(MicProfile micProfile)
        : this(micProfile.Name, micProfile.ChannelIndex)
    {
    }
}
