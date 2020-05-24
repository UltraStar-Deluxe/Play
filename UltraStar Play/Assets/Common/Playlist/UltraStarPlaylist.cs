using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class UltraStarPlaylist
{
    private readonly List<UltraStartPlaylistLineEntry> lineEntries = new List<UltraStartPlaylistLineEntry>();
    public IReadOnlyList<UltraStartPlaylistLineEntry> LineEntries => lineEntries;

    private readonly List<UltraStartPlaylistSongEntry> songEntries = new List<UltraStartPlaylistSongEntry>();
    public IReadOnlyList<UltraStartPlaylistSongEntry> SongEntries => songEntries;

    public void AddLineEntry(UltraStartPlaylistLineEntry lineEntry)
    {
        lineEntries.Add(lineEntry);
        if (lineEntry is UltraStartPlaylistSongEntry songEntry)
        {
            songEntries.Add(songEntry);
        }
    }

    public void RemoveSongEntry(string artist, string title)
    {
        UltraStartPlaylistSongEntry songEntry = songEntries
            .Where(it => it.Artist == artist.Trim() && it.Title == title.Trim()).FirstOrDefault();
        if (songEntry != null)
        {
            lineEntries.Remove(songEntry);
            songEntries.Remove(songEntry);
        }
    }

    public virtual bool HasSongEntry(string artist, string title)
    {
        UltraStartPlaylistSongEntry songEntry = songEntries
            .Where(it => it.Artist == artist.Trim() && it.Title == title.Trim()).FirstOrDefault();
        return songEntry != null;
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
