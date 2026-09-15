using UnityEngine;

public interface IBackgroundUriProvider : IMod
{
    public Awaitable<string> GetBackgroundUriAsync(SongMeta songMeta);
}
