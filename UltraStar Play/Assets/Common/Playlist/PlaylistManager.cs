using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class PlaylistManager : AbstractSingletonBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        playlistToFilePathMap.Clear();
        playlists.Clear();
        favoritesPlaylist = new UltraStarPlaylist();
    }

    public static PlaylistManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<PlaylistManager>();

    public static readonly string favoritesPlaylistName = "Favorites";

    // static references to be persisted across scenes
    private static Dictionary<UltraStarPlaylist, string> playlistToFilePathMap = new();
    private static List<UltraStarPlaylist> playlists = new();
    private static UltraStarPlaylist favoritesPlaylist = new();

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

    public UltraStarPlaylist FavoritesPlaylist
    {
        get
        {
            if (playlists.IsNullOrEmpty())
            {
                CreateFavoritePlaylistIfNotExist();
                ScanPlaylistFolder();
            }
            return favoritesPlaylist;
        }
    }

    private readonly Subject<PlaylistChangeEvent> playlistChangeEventStream = new();
    public IObservable<PlaylistChangeEvent> PlaylistChangeEventStream => playlistChangeEventStream;

    private readonly string playlistFileExtension = ".playlist";
    private string FavoritesPlaylistFile => $"{PlaylistFolder}/{favoritesPlaylistName}{playlistFileExtension}";
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
        if (!File.Exists(FavoritesPlaylistFile))
        {
            File.WriteAllText(FavoritesPlaylistFile, "# UltraStar playlist");
        }
    }

    public void SavePlaylist(UltraStarPlaylist playlist)
    {
        string[] lines = playlist.GetLines();
        string filePath = GetFilePathForPlaylist(playlist);
        File.WriteAllLines(filePath, lines);
    }

    private string GetFilePathForPlaylist(UltraStarPlaylist playlist)
    {
        if (playlist == FavoritesPlaylist)
        {
            return FavoritesPlaylistFile;
        }
        return playlistToFilePathMap[playlist];
    }

    private void ScanPlaylistFolder()
    {
        playlists = new List<UltraStarPlaylist>();
        playlistToFilePathMap = new Dictionary<UltraStarPlaylist, string>();

        FolderScanner scanner = new("*" + playlistFileExtension);
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
            using (FileStream fileStream = File.Create(filePath))
            {
                // Automatically closed by using-statement.
            }
        }

        playlists.Add(playlist);
        playlistToFilePathMap.Add(playlist, filePath);

        if (Path.GetFullPath(FavoritesPlaylistFile) == Path.GetFullPath(filePath))
        {
            // This is the special playlist for the favorite songs.
            favoritesPlaylist = playlist;
        }
    }

    public string GetPlaylistName(UltraStarPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist)
        {
            return "";
        }

        string filePath = playlistToFilePathMap[playlist];
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }

    public void RemoveSongFromPlaylist(UltraStarPlaylist playlist, SongMeta songMeta)
    {
        playlist.RemoveSongEntry(songMeta.Artist, songMeta.Title);
        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, songMeta));
        SavePlaylist(playlist);
    }

    public void AddSongToPlaylist(UltraStarPlaylist playlist, SongMeta songMeta)
    {
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
            .Select(it => GetPlaylistName(it))
            .AnyMatch(playlistName => playlistName == newName))
        {
            return EPlaylistNameIssue.Duplicate;
        }

        return EPlaylistNameIssue.None;
    }

    public string TrySetPlaylistName(UltraStarPlaylist playlist, string newName)
    {
        if (GetPlaylistName(playlist) == newName)
        {
            return "";
        }

        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist
            || GetPlaylistName(playlist) == favoritesPlaylistName)
        {
            return "Cannot rename this playlist";
        }

        if (GetPlaylistNameIssue(playlist, newName) != EPlaylistNameIssue.None)
        {
            return "Invalid or duplicate playlist name";
        }

        // Rename file
        if (!playlistToFilePathMap.TryGetValue(playlist, out string oldPath))
        {
            return "Playlist not found in file system";
        }

        string oldName = GetPlaylistName(playlist);
        string oldFolder = Path.GetDirectoryName(oldPath);
        string newPath = $"{oldFolder}/{newName}{playlistFileExtension}";
        try
        {
            Debug.Log($"Moving playlist from '{oldPath}' to '{newPath}'");
            File.Move(oldPath, newPath);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, $"Failed to rename playlist to {newName}");
            return "Failed to rename playlist: " + e.Message;
        }

        // Update path in map such that modifications of playlist will be saved to correct location.
        playlistToFilePathMap[playlist] = newPath;

        // Update settings
        if (settings.SongSelectSettings.playlistName == oldName)
        {
            settings.SongSelectSettings.playlistName = newName;
        }

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        return "";
    }

    public string TryRemovePlaylist(UltraStarPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist
            || GetPlaylistName(playlist) == favoritesPlaylistName)
        {
            return "Cannot remove this playlist";
        }

        if (!playlistToFilePathMap.TryGetValue(playlist, out string oldPath))
        {
            return "Playlist not found in file system";
        }

        string oldName = GetPlaylistName(playlist);
        try
        {
            Debug.Log($"Deleting playlist '{oldPath}'");
            File.Delete(oldPath);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, $"Failed to delete playlist '{oldPath}'");
            return "Failed to delete playlist: " + e.Message;
        }

        // Update settings
        if (settings.SongSelectSettings.playlistName == oldName)
        {
            settings.SongSelectSettings.playlistName = "";
        }

        playlists.Remove(playlist);
        playlistToFilePathMap.Remove(playlist);


        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        return "";
    }

    public UltraStarPlaylist CreateNewPlaylist(string initialName)
    {
        string newPlaylistName = GetNewUniquePlaylistName(initialName);
        string newPlaylistPath = $"{PlaylistFolder}/{newPlaylistName}{playlistFileExtension}";
        UltraStarPlaylist newPlaylist = new();
        AddPlaylist(newPlaylist, newPlaylistPath);

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(newPlaylist, null));

        return newPlaylist;
    }

    private string GetNewUniquePlaylistName(string initialName)
    {
        bool IsPlaylistNameUnique(string playlistName)
        {
            return Playlists
                .AllMatch(playlist => GetPlaylistName(playlist) != playlistName);
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
