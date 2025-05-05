public class SongDto : JsonSerializable
{
    public string Artist { get; set; }
    public string Title { get; set; }
    public string Hash { get; set; }
}
