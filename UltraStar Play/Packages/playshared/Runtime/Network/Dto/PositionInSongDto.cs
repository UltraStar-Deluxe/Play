public class PositionInSongDto : CompanionAppMessageDto
{
    public float SongBpm { get; set; }
    public float SongGap { get; set; }
    public double PositionInSongInMillis { get; set; }
    public double UnixTimeInMillis { get; set; }

    public PositionInSongDto() : base(CompanionAppMessageType.PositionInSong) { }
}
