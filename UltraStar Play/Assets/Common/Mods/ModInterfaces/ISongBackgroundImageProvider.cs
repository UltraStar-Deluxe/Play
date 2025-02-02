using UnityEngine;

public interface ISongBackgroundImageProvider : IMod
{
    public Awaitable<string> GetBackgroundImageUriAsync(SongMeta songMeta);
}
