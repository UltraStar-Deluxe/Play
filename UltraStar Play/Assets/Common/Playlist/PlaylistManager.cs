using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProTrans;
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
    private static List<IPlaylist> playlists = new();

    public IReadOnlyList<IPlaylist> Playlists
    {
        get
        {
            if (playlists.IsNullOrEmpty())
            {
                CreateFavoritePlaylistIfNotExist();
                ScanPlaylists();
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
                ScanPlaylists();
            }
            return favoritesPlaylist;
        }
    }

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly Subject<PlaylistChangeEvent> playlistChangeEventStream = new();
    public IObservable<PlaylistChangeEvent> PlaylistChangeEventStream => playlistChangeEventStream;

    private string favoritesPlaylistFilePath;
    private string playlistFolder;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        playlistFolder = $"{Application.persistentDataPath}/Playlists";
        favoritesPlaylistFilePath = $"{playlistFolder}/{favoritesPlaylistName}.{ApplicationUtils.ultraStarPlaylistFileExtension}";
        CreateFavoritePlaylistIfNotExist();
    }

    private void CreateFavoritePlaylistIfNotExist()
    {
        if (!Directory.Exists(playlistFolder))
        {
            Directory.CreateDirectory(playlistFolder);
        }
        if (!File.Exists(favoritesPlaylistFilePath))
        {
            File.WriteAllText(favoritesPlaylistFilePath, "# UltraStar playlist");
        }
    }

    public bool IsFavoritesPlaylist(IPlaylist playlist)
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

    private void ScanPlaylists()
    {
        Debug.Log("Scanning playlists");
        using DisposableStopwatch d = new("Scanning playlists took <ms> ms");

        playlists = new List<IPlaylist>();

        ScanPlaylistsInFolder(playlistFolder);

        // Scan for playlists in song folders on background thread.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            List<string> songFolders = SettingsUtils.GetEnabledSongFolders(settings);
            foreach (string songFolder in songFolders)
            {
                ScanPlaylistsInFolder(songFolder);
            }
        });
    }

    private void ScanPlaylistsInFolder(string folder)
    {
        Debug.Log($"Scanning playlists in folder '{folder}'");
        using DisposableStopwatch d2 = new($"Scanning playlists in folder '{folder}' took <ms> ms");

        ScanUltraStarPlaylistsInFolder(folder);
        ScanM3UPlaylistsInFolder(folder);
    }

    private void ScanM3UPlaylistsInFolder(string folder)
    {
        FileScanner scanner = new($"*.{ApplicationUtils.m3uPlaylistFileExtension}", true, true);
        List<string> playlistFilePaths = scanner.GetFiles(folder, true);
        foreach (string filePath in playlistFilePaths)
        {
            M3UPlaylist playlist = M3UPlaylistParser.ParseFile(filePath);
            AddPlaylist(playlist, filePath);
        }
    }

    private void ScanUltraStarPlaylistsInFolder(string folder)
    {
        string ultraStarPlaylistFileExtensionPattern = $"*.{ApplicationUtils.ultraStarPlaylistFileExtension}";
        FileScanner scanner = new(ultraStarPlaylistFileExtensionPattern, true, true);
        List<string> playlistFilePaths = scanner.GetFiles(folder, true);
        foreach (string filePath in playlistFilePaths)
        {
            UltraStarPlaylist playlist = UltraStarPlaylistParser.ParseFile(filePath);
            AddPlaylist(playlist, filePath);
        }
    }

    private void AddPlaylist(IPlaylist playlist, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Cannot add playlist because its file does not exist: '{filePath}'");
            return;
        }

        playlists.Add(playlist);

        if (playlist is UltraStarPlaylist
            && Path.GetFullPath(favoritesPlaylistFilePath) == Path.GetFullPath(filePath))
        {
            // This is the special playlist for the favorite songs.
            favoritesPlaylist = playlist as UltraStarPlaylist;
        }
    }

    public void RemoveSongFromPlaylist(UltraStarPlaylist playlist, SongMeta songMeta)
    {
        if (playlist == null
            || songMeta == null)
        {
            return;
        }
        playlist.RemoveSongEntry(songMeta.Artist, songMeta.Title);
        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, songMeta));
        SavePlaylist(playlist);
    }

    public void AddSongToPlaylist(UltraStarPlaylist playlist, SongMeta songMeta)
    {
        if (playlist == null
            || songMeta == null
            || HasSongEntry(playlist, songMeta))
        {
            return;
        }
        playlist.AddLineEntry(new UltraStartPlaylistSongEntry(songMeta.Artist, songMeta.Title));
        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, songMeta));
        SavePlaylist(playlist);
    }

    public class PlaylistChangeEvent
    {
        public IPlaylist Playlist { get; set; }
        public SongMeta SongMeta { get; set; }

        public PlaylistChangeEvent(IPlaylist playlist, SongMeta songMeta)
        {
            Playlist = playlist;
            SongMeta = songMeta;
        }
    }

    public EPlaylistNameIssue GetPlaylistNameIssue(IPlaylist playlist, string newName)
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

    public bool TrySetPlaylistName(IPlaylist playlist, string newName, out string errorMessage)
    {
        if (playlist == null
            || playlist.Name == newName)
        {
            errorMessage = "";
            return true;
        }

        UltraStarPlaylist ultraStarPlaylist = playlist as UltraStarPlaylist;
        if (playlist is UltraStarAllSongsPlaylist
            || playlist.Name == favoritesPlaylistName
            || playlist.FilePath.IsNullOrEmpty()
            || ultraStarPlaylist == null)
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
        string newPath = $"{oldFolder}/{newName}.{ApplicationUtils.ultraStarPlaylistFileExtension}";
        try
        {
            Debug.Log($"Moving playlist from '{oldPath}' to '{newPath}'");
            File.Move(oldPath, newPath);
            ultraStarPlaylist.SetFileName(newName);
            ultraStarPlaylist.RemoveHeaderField("name");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to rename playlist to '{newName}': {e.Message}");
            errorMessage = $"Failed to rename playlist to '{newName}': " + e.Message;
            return false;
        }

        // Update settings
        if (nonPersistentSettings != null
            && nonPersistentSettings.PlaylistName.Value == oldName)
        {
            nonPersistentSettings.PlaylistName.Value = newName;
        }

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        errorMessage = "";
        return true;
    }

    public string TryRemovePlaylist(IPlaylist playlist)
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
            Debug.LogException(e);
            Debug.LogError($"Failed to delete playlist '{oldName}': {e.Message}");
            return $"Failed to delete playlist '{oldName}': " + e.Message;
        }

        // Update settings
        if (nonPersistentSettings != null
            && nonPersistentSettings.PlaylistName.Value == oldName)
        {
            nonPersistentSettings.PlaylistName.Value = "";
        }

        playlists.Remove(playlist);

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        return "";
    }

    public UltraStarPlaylist CreateNewPlaylist(string initialName)
    {
        string newPlaylistName = GetNewUniquePlaylistName(initialName);
        string newPlaylistPath = $"{playlistFolder}/{newPlaylistName}.{ApplicationUtils.ultraStarPlaylistFileExtension}";

        // Create playlist file
        File.WriteAllText(newPlaylistPath, "# UltraStar playlist");
        FileUtils.SleepUntilFileExists(newPlaylistPath, 100);

        // Create playlist object
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

    public bool HasSongEntry(IPlaylist playlist, SongMeta songMeta)
    {
        return playlist.HasSongEntry(songMeta);
    }

    public List<SongMeta> GetSongMetas(IPlaylist playlist)
    {
        IReadOnlyCollection<SongMeta> allSongMetas = songMetaManager.GetSongMetas();
        return allSongMetas.Where(songMeta => HasSongEntry(playlist, songMeta)).ToList();
    }

    public IPlaylist GetPlaylistByName(string playlistName)
    {
        return playlists.FirstOrDefault(playlist => GetPlaylistName(playlist) == playlistName);
    }

    public List<IPlaylist> GetPlaylists(bool includeAllSongPlaylist, bool includeFavoritesPlaylist)
    {
        List<IPlaylist> result = new();
        if (includeFavoritesPlaylist)
        {
            result.Add(UltraStarAllSongsPlaylist.Instance);
        }

        result.AddRange(Playlists);

        if (!includeFavoritesPlaylist)
        {
            result.Remove(FavoritesPlaylist);
        }
        return result;
    }

    public string GetPlaylistName(IPlaylist playlist)
    {
        if (playlist == null)
        {
            return "";
        }

        if (playlist is UltraStarAllSongsPlaylist)
        {
            return TranslationManager.GetTranslation(R.Messages.playlistName_allSongs);
        }

        return playlist.Name;
    }
}
