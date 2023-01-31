using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class PlaylistManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        playlists.Clear();
    }

    public static PlaylistManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<PlaylistManager>();

    public static readonly string favoritesPlaylistName = "Favorites";

    // static references to be persisted across scenes
    private static List<UltraStarPlaylist> playlists = new();

    public IReadOnlyList<UltraStarPlaylist> Playlists
    {
        get
        {
            if (playlists.IsNullOrEmpty())
            {
                CreateFavoritePlaylistIfNotExist();
                ScanPlaylistFolder();
            }
            return playlists;
        }
    }

    private UltraStarPlaylist favoritesPlaylist;
    public UltraStarPlaylist FavoritesPlaylist
    {
        get
        {
            if (favoritesPlaylist == null
                || playlists.IsNullOrEmpty())
            {
                CreateFavoritePlaylistIfNotExist();
                ScanPlaylistFolder();
            }
            return favoritesPlaylist;
        }
    }

    private readonly Subject<PlaylistChangeEvent> playlistChangeEventStream = new();
    public IObservable<PlaylistChangeEvent> PlaylistChangeEventStream => playlistChangeEventStream;

    public static readonly string ultraStarPlaylistFileExtension = ".upl";
    private string FavoritesPlaylistFilePath => $"{PlaylistFolder}/{favoritesPlaylistName}{ultraStarPlaylistFileExtension}";
    private string PlaylistFolder => $"{Application.persistentDataPath}/Playlists";

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        CreateFavoritePlaylistIfNotExist();
    }

    private void CreateFavoritePlaylistIfNotExist()
    {
        if (!Directory.Exists(PlaylistFolder))
        {
            Directory.CreateDirectory(PlaylistFolder);
        }
        if (!File.Exists(FavoritesPlaylistFilePath))
        {
            File.WriteAllText(FavoritesPlaylistFilePath, "# UltraStar playlist");
        }
    }

    public bool IsFavoritesPlaylist(UltraStarPlaylist playlist)
    {
        return playlist.Name == favoritesPlaylistName;
    }

    public void SavePlaylist(UltraStarPlaylist playlist)
    {
        if (playlist.FilePath.IsNullOrEmpty())
        {
            return;
        }
        string[] lines = playlist.GetLines();
        File.WriteAllLines(playlist.FilePath, lines);
    }

    private void ScanPlaylistFolder()
    {
        playlists = new List<UltraStarPlaylist>();

        FolderScanner scanner = new("*" + ultraStarPlaylistFileExtension);
        List<string> playlistFilePaths = scanner.GetFiles(PlaylistFolder);
        foreach (string filePath in playlistFilePaths)
        {
            UltraStarPlaylist playlist = UltraStarPlaylistParser.ParseFile(filePath);
            AddPlaylist(playlist, filePath);
        }
    }

    private void AddPlaylist(UltraStarPlaylist playlist, string filePath)
    {
        if (!File.Exists(filePath))
        {
            // Create empty file
            using (FileStream fileStream = File.Create(filePath))
            {
                // Automatically closed by using-statement.
            }
        }

        playlists.Add(playlist);

        if (Path.GetFullPath(FavoritesPlaylistFilePath) == Path.GetFullPath(filePath))
        {
            // This is the special playlist for the favorite songs.
            favoritesPlaylist = playlist;
        }
    }

    public void RemoveSongFromPlaylist(UltraStarPlaylist playlist, SongMeta songMeta)
    {
        playlist.RemoveSongEntry(songMeta.Artist, songMeta.Title);
        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, songMeta));
        SavePlaylist(playlist);
    }

    public void AddSongToPlaylist(UltraStarPlaylist playlist, SongMeta songMeta)
    {
        if (HasSongEntry(playlist, songMeta))
        {
            return;
        }
        playlist.AddLineEntry(new UltraStartPlaylistSongEntry(songMeta.Artist, songMeta.Title));
        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, songMeta));
        SavePlaylist(playlist);
    }

    public class PlaylistChangeEvent
    {
        public UltraStarPlaylist Playlist { get; set; }
        public SongMeta SongMeta { get; set; }

        public PlaylistChangeEvent(UltraStarPlaylist playlist, SongMeta songMeta)
        {
            Playlist = playlist;
            SongMeta = songMeta;
        }
    }

    public EPlaylistNameIssue GetPlaylistNameIssue(UltraStarPlaylist playlist, string newName)
    {
        if (newName.IsNullOrEmpty())
        {
            return EPlaylistNameIssue.Invalid;
        }

        List<char> invalidCharacters = Path.GetInvalidPathChars()
            .Concat(new List<char> { '\\', '/' })
            .ToList();
        foreach (char invalidChar in invalidCharacters)
        {
            if (newName.Contains(invalidChar))
            {
                return EPlaylistNameIssue.Invalid;
            }
        }

        if (playlists
            .Where(it => it != playlist)
            .Select(it => it.Name)
            .AnyMatch(playlistName => playlistName == newName))
        {
            return EPlaylistNameIssue.Duplicate;
        }

        return EPlaylistNameIssue.None;
    }

    public bool TrySetPlaylistName(UltraStarPlaylist playlist, string newName, out string errorMessage)
    {
        if (playlist == null
            || playlist.Name == newName)
        {
            errorMessage = "";
            return true;
        }

        if (playlist is UltraStarAllSongsPlaylist
            || playlist.Name == favoritesPlaylistName
            || playlist.FilePath.IsNullOrEmpty())
        {
            errorMessage = "Cannot rename this playlist";
            return false;
        }

        if (GetPlaylistNameIssue(playlist, newName) != EPlaylistNameIssue.None)
        {
            errorMessage = "Invalid or duplicate playlist name";
            return false;
        }

        // Rename file
        string oldName = playlist.Name;
        string oldPath = playlist.FilePath;
        string oldFolder = Path.GetDirectoryName(playlist.FilePath);
        string newPath = $"{oldFolder}/{newName}{ultraStarPlaylistFileExtension}";
        try
        {
            Debug.Log($"Moving playlist from '{oldPath}' to '{newPath}'");
            File.Move(oldPath, newPath);
            playlist.SetFileName(newName);
            playlist.RemoveHeaderField("name");
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, $"Failed to rename playlist to '{newName}'");
            errorMessage = $"Failed to rename playlist to '{newName}': " + e.Message;
            return false;
        }

        // Update settings
        if (settings.SongSelectSettings.playlistName == oldName)
        {
            settings.SongSelectSettings.playlistName = newName;
        }

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        errorMessage = "";
        return true;
    }

    public string TryRemovePlaylist(UltraStarPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist
            || playlist.Name == favoritesPlaylistName
            || playlist.FilePath.IsNullOrEmpty())
        {
            return "Cannot remove this playlist";
        }

        string oldName = playlist.Name;
        try
        {
            Debug.Log($"Deleting playlist '{oldName}'");
            File.Delete(playlist.FilePath);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, $"Failed to delete playlist '{oldName}'");
            return $"Failed to delete playlist '{oldName}': " + e.Message;
        }

        // Update settings
        if (settings.SongSelectSettings.playlistName == oldName)
        {
            settings.SongSelectSettings.playlistName = "";
        }

        playlists.Remove(playlist);

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        return "";
    }

    public UltraStarPlaylist CreateNewPlaylist(string initialName)
    {
        string newPlaylistName = GetNewUniquePlaylistName(initialName);
        string newPlaylistPath = $"{PlaylistFolder}/{newPlaylistName}{ultraStarPlaylistFileExtension}";
        UltraStarPlaylist newPlaylist = new(newPlaylistPath);
        AddPlaylist(newPlaylist, newPlaylistPath);

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(newPlaylist, null));

        return newPlaylist;
    }

    private string GetNewUniquePlaylistName(string initialName)
    {
        bool IsPlaylistNameUnique(string playlistName)
        {
            return Playlists.AllMatch(playlist => playlist.Name != playlistName)
                   && Playlists.AllMatch(playlist => playlist.FileName != playlistName);
        }

        int index = 1;
        string newPlaylistName = initialName;
        while (!IsPlaylistNameUnique(newPlaylistName))
        {
            index++;
            newPlaylistName = $"{initialName} {index}";
        }

        return newPlaylistName;
    }

    public bool HasSongEntry(UltraStarPlaylist playlist, SongMeta songMeta)
    {
        return playlist.HasSongEntry(songMeta.Artist, songMeta.Title);
    }
}
