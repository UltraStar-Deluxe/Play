using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UltraStarPlaylist
{
    private readonly List<UltraStartPlaylistLineEntry> lineEntries = new List<UltraStartPlaylistLineEntry>();
    private readonly HashSet<string> songHashes = new HashSet<string>();

    public string[] GetLines()
    {
        return lineEntries.Select(it => it.Line).ToArray();
    }

    public void AddLineEntry(UltraStartPlaylistLineEntry lineEntry)
    {
        lineEntries.Add(lineEntry);
        if (lineEntry is UltraStartPlaylistSongEntry songEntry)
        {
            songHashes.Add(GetHash(songEntry.Artist, songEntry.Title));
        }
    }

    public void RemoveSongEntry(string artist, string title)
    {
        UltraStartPlaylistLineEntry lineEntry = lineEntries
            .Find(it => (it is UltraStartPlaylistSongEntry songEntry)
                        && songEntry.Artist == artist.Trim()
                        && songEntry.Title == title.Trim());
        if (lineEntry != null)
        {
            lineEntries.Remove(lineEntry);
            songHashes.Remove(GetHash(artist, title));
        }
    }

    public virtual bool HasSongEntry(string artist, string title)
    {
        return songHashes.Contains(GetHash(artist, title));
    }

    private string GetHash(string artist, string title)
    {
        return artist.Trim() + "-" + title.Trim();
    }
}

public class UltraStartPlaylistLineEntry
{
    public string Line { get; private set; }

    public UltraStartPlaylistLineEntry(string line)
    {
        Line = line;
    }
}

public class UltraStartPlaylistSongEntry : UltraStartPlaylistLineEntry
{
    public string Artist { get; private set; }
    public string Title { get; private set; }

    public UltraStartPlaylistSongEntry(string artist, string title)
        : base(QuoteIfNeeded(artist) + $" {UltraStarPlaylistParser.separator} " + QuoteIfNeeded(title))
    {
        Artist = artist;
        Title = title;
    }

    public UltraStartPlaylistSongEntry(string line, string artist, string title)
        : base(line)
    {
        Artist = artist;
        Title = title;
    }

    private static string QuoteIfNeeded(string text)
    {
        return text.Contains(UltraStarPlaylistParser.separator)
            ? $"\"{text}\""
            : text;
    }
}
