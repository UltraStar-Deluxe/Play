using System.Collections.Generic;

public interface ISongRepository : IMod
{
    public List<SongMetaProxy> SearchSongs(string searchTerm);
    public List<SongMetaProxy> GetDefaultSongs();
    public List<SongMetaProxy> GetRandomSongs();
}
