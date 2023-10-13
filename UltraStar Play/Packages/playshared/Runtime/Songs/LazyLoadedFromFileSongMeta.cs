using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class LazyLoadedFromFileSongMeta : LazyLoadedSongMeta
{
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

        OnLoadSong = () =>
        {
            using IDisposable d = new DisposableStopwatch($"Loading '{fileInfo.Name}' took <ms> ms");
            UltraStarSongMeta loadedSongMeta = UltraStarSongParser.ParseFile(fileInfo.FullName, out List<SongIssue> songIssues, FileEncoding);
            CopyValues(loadedSongMeta);
        };

        OnLoadVoices = () =>
        {
            using IDisposable d = new DisposableStopwatch($"Loading voices of '{fileInfo.Name}' took <ms> ms");
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
            return;
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

        artist = parts[0];
        title = parts[1];
        return true;
    }
}
