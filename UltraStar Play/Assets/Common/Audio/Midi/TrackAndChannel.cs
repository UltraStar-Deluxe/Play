public class TrackAndChannel
{
    public int trackIndex;
    public int channelIndex;
        
    public TrackAndChannel(int trackIndex, int channelIndex)
    {
        this.trackIndex = trackIndex;
        this.channelIndex = channelIndex;
    }

    public override string ToString()
    {
        return $"track {trackIndex}, channel {channelIndex}";
    }
}
