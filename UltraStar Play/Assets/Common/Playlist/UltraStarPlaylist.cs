using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UltraStarPlaylist
{
    public string FilePath { get; private set; }
    public string FileName => Path.GetFileNameWithoutExtension(FilePath);
    public string Name
    {
        get
        {
            string headerName = GetHeaderValue("name");
            if (!headerName.IsNullOrEmpty())
            {
                return headerName;
            }
            return FileName;
        }
    }

    private readonly List<UltraStartPlaylistLineEntry> lineEntries = new();
    private readonly HashSet<string> songHashes = new();
    private readonly Dictionary<string, string> headerFields = new();

    public UltraStarPlaylist(string filePath)
    {
        FilePath = filePath;
    }

    public string GetHeaderValue(string headerName)
    {
        if (headerFields.TryGetValue(headerName.ToUpperInvariant(), out string headerValue))
        {
            return headerValue;
        }

        return "";
    }

    public void SetFileName(string newValue)
    {
        string directoryPath = Path.GetDirectoryName(FilePath);
        FilePath = directoryPath + $"/{newValue}{PlaylistManager.ultraStarPlaylistFileExtension}";
        headerFields.Remove("NAME");
    }

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
        else if (lineEntry is UltraStartPlaylistHeaderEntry headerEntry)
        {
            headerFields[headerEntry.HeaderName] = headerEntry.HeaderValue;
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

    public List<UltraStartPlaylistSongEntry> GetSongEntries()
    {
        return lineEntries
            .OfType<UltraStartPlaylistSongEntry>()
            .ToList();
    }

    public virtual bool HasSongEntry(string artist, string title)
    {
        return songHashes.Contains(GetHash(artist, title));
    }

    private string GetHash(string artist, string title)
    {
        return artist.Trim() + "-" + title.Trim();
    }

    public void RemoveHeaderField(string headerName)
    {
        headerFields.Remove(headerName.ToUpperInvariant());
        UltraStartPlaylistLineEntry lineEntry = lineEntries
            .FirstOrDefault(it => it is UltraStartPlaylistHeaderEntry headerEntry
                                  && headerEntry.HeaderName == headerName.ToUpperInvariant());
        lineEntries.Remove(lineEntry);
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

public class UltraStartPlaylistHeaderEntry : UltraStartPlaylistLineEntry
{
    public string HeaderName { get; private set; }
    public string HeaderValue { get; private set; }

    public UltraStartPlaylistHeaderEntry(string line, string headerName, string headerValue)
        : base(line)
    {
        HeaderName = headerName.ToUpperInvariant().Trim();
        HeaderValue = headerValue.Trim();
    }
}

public class UltraStartPlaylistSongEntry : UltraStartPlaylistLineEntry
{
    public string Artist { get; private set; }
    public string Title { get; private set; }

    public UltraStartPlaylistSongEntry(string line, string artist, string title)
        : base(line)
    {
        Artist = artist;
        Title = title;
    }

    public UltraStartPlaylistSongEntry(string artist, string title)
        : this(QuoteIfNeeded(artist) + $" {UltraStarPlaylistParser.defaultSeparator} " + QuoteIfNeeded(title),
            artist,
            title)
    {
        Artist = artist;
        Title = title;
    }

    private static string QuoteIfNeeded(string text)
    {
        return UltraStarPlaylistParser.separators.AnyMatch(separator => text.Contains(separator))
            ? $"\"{text}\""
            : text;
    }
}
