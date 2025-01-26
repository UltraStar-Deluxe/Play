using UnityEngine;

public interface ISongCoverImageProvider : IMod
{
    public Awaitable<string> GetCoverImageUriAsync(SongMeta songMeta);
}
