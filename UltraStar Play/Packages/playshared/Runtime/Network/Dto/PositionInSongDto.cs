public class PositionInSongDto : CompanionAppMessageDto
{
    public float SongBpm { get; set; }
    public float SongGap { get; set; }
    public int PositionInSongInMillis { get; set; }

    public PositionInSongDto() : base(CompanionAppMessageType.PositionInSong) { }
}
