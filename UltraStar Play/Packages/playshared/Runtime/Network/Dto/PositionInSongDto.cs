public class PositionInSongDto : CompanionAppMessageDto
{
    public double SongBpm { get; set; }
    public double SongGap { get; set; }
    public int PositionInSongInMillis { get; set; }

    public PositionInSongDto() : base(CompanionAppMessageType.PositionInSong) { }
}
