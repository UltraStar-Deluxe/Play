public class SongQueueEntryDto : JsonSerializable
{
    public SongDto SongDto { get; set; }
    public SingScenePlayerDataDto SingScenePlayerDataDto { get; set; } = new();
    public GameRoundSettingsDto GameRoundSettingsDto { get; set; } = new();
    public bool IsMedleyWithPreviousEntry { get; set; }
}
