using System;

public class SongSelectSongEntry : SongSelectEntry
{
    public SongMeta SongMeta { get; private set; }

    public SongSelectSongEntry(SongMeta songMeta)
    {
        SongMeta = songMeta ?? throw new ArgumentNullException(nameof(songMeta));
    }

    protected bool Equals(SongSelectSongEntry other)
    {
        return Equals(SongMeta, other.SongMeta);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((SongSelectSongEntry)obj);
    }

    public override int GetHashCode()
    {
        return SongMeta.GetHashCode();
    }
}
