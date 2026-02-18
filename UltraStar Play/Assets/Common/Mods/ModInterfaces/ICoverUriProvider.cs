using UnityEngine;

public interface ICoverUriProvider : IMod
{
    public Awaitable<string> GetCoverUriAsync(SongMeta songMeta);
}
