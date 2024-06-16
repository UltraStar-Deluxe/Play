using System;
using System.Collections.Generic;

public class SongListSongDtoComparer : IComparer<SongDto>
{
    private readonly NullOrEmptyValueLastComparer nullOrEmptyValueLastComparer = new();
    
    public int Compare(SongDto x, SongDto y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (ReferenceEquals(null, y))
        {
            return 1;
        }

        if (ReferenceEquals(null, x))
        {
            return -1;
        }

        int artistComparison = nullOrEmptyValueLastComparer.Compare(x.Artist, y.Artist);
        if (artistComparison != 0)
        {
            return artistComparison;
        }

        int titleComparison = nullOrEmptyValueLastComparer.Compare(x.Title, y.Title);
        if (titleComparison != 0)
        {
            return titleComparison;
        }

        return string.Compare(x.Hash, y.Hash, StringComparison.InvariantCultureIgnoreCase);
    }
}
