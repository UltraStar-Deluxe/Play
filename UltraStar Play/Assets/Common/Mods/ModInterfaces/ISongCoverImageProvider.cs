using UnityEngine;

public interface ISongCoverImageProvider : IMod
{
    public Awaitable<string> GetCoverImageUri(SongMeta songMeta);
}
