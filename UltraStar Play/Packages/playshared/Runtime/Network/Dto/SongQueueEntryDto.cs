public class SongQueueEntryDto : JsonSerializable
{
    public SongDto SongDto { get; set; }
    public SingScenePlayerDataDto SingScenePlayerDataDto { get; set; } = new();
    public GameRoundSettings GameRoundSettings { get; set; } = new();
    public bool IsMedleyWithPreviousEntry { get; set; }
}
