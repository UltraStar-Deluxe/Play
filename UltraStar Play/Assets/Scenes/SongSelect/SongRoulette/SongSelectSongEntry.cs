using System;

public class SongSelectSongEntry : SongSelectEntry
{
    public SongMeta SongMeta { get; private set; }

    public SongSelectSongEntry(SongMeta songMeta)
    {
        SongMeta = songMeta ?? throw new ArgumentNullException(nameof(songMeta));
    }
}
