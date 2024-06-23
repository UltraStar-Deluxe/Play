using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class LazyLoadedFromFileSongMeta : UltraStarSongMeta, IHasSongIssues
{
    public List<SongIssue> SongIssues { get; private set; } = new();

    public LazyLoadedFromFileSongMeta(string filePath, Encoding encoding = null)
        : this(new FileInfo(filePath), encoding)
    {
    }

    public LazyLoadedFromFileSongMeta(FileInfo fileInfo, Encoding encoding = null)
    {
        if (fileInfo == null
            || !fileInfo.Exists)
        {
            throw new IllegalArgumentException($"File does not exist: {fileInfo}");
        }

        SetFileInfo(fileInfo, encoding);

        DoLoadSong = () =>
        {
            using IDisposable d = new DisposableStopwatch($"Loading '{fileInfo.Name}' took <ms> ms", ELogEventLevel.Verbose);
            UltraStarSongMeta loadedSongMeta = UltraStarSongParser.ParseFile(fileInfo.FullName, out List<SongIssue> songIssues, FileEncoding);
            CopyValues(loadedSongMeta);

            SongIssues = songIssues;
        };

        DoLoadVoices = () =>
        {
            using IDisposable d = new DisposableStopwatch($"Loading voices of '{fileInfo.Name}' took <ms> ms", ELogEventLevel.Verbose);
            List<Voice> voices = UltraStarSongVoicesParser.ParseFile(
                FileInfo.FullName,
                FileEncoding,
                false,
                false);
            voices.ForEach(voice => AddVoice(voice));
        };

        // Check whether the file name or its directory
        // matches the 'artist - title' convention.
        if (TrySplitArtistAndTitle(Path.GetFileNameWithoutExtension(fileInfo.Name), out string artistFromFileName, out string titleFromFileName))
        {
            Artist = artistFromFileName;
            Title = titleFromFileName;
            return;
        }

        if (TrySplitArtistAndTitle(fileInfo.Directory.Name, out string artistFromFolderName, out string titleFromFolderName))
        {
            Artist = artistFromFolderName;
            Title = titleFromFolderName;
        }
    }

    private bool TrySplitArtistAndTitle(string fileName, out string artist, out string title)
    {
        string separator = " - ";
        if (!fileName.Contains(separator))
        {
            artist = "";
            title = "";
            return false;
        }

        string[] parts = fileName.Split(separator);
        if (parts.Length != 2)
        {
            artist = "";
            title = "";
            return false;
        }

        artist = parts[0].OrIfNull("");
        title = parts[1].OrIfNull("");
        return true;
    }
}
