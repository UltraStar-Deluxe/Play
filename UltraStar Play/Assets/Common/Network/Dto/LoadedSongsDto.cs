using System.Collections.Generic;

public class LoadedSongsDto : JsonSerializable
{
    public bool isSongScanFinished;
    public int songCount;
    public List<SongDto> songList;
}
