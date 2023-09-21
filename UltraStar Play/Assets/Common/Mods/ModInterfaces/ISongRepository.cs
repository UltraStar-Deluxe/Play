using System.Collections.Generic;

public interface ISongRepository : IMod
{
    public List<SongMeta> SearchSongs(string searchTerm);
    public List<SongMeta> GetDefaultSongs();
    public List<SongMeta> GetRandomSongs();
}
