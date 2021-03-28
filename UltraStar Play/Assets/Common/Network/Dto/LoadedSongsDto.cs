using System.Collections.Generic;

public class LoadedSongsDto : JsonSerializable
{
    public bool IsSongScanFinished { get; set; }
    public int SongCount { get; set; }
    public List<SongDto> SongList { get; set; }
}
