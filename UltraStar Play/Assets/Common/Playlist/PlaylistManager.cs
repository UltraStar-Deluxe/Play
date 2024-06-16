using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniInject;
using UniRx;
using UnityEngine;

public class PlaylistManager : AbstractSingletonBehaviour, INeedInjection
{
    public static PlaylistManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<PlaylistManager>();

    public static readonly string favoritesPlaylistName = "Favorites";

    private List<IPlaylist> playlists = new();

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

    private readonly Subject<PlaylistChangeEvent> playlistChangeEventStream = new();
    public IObservable<PlaylistChangeEvent> PlaylistChangeEventStream => playlistChangeEventStream
        .ObserveOnMainThread();

    private string favoritesPlaylistFilePath;
    private string playlistFolder;

    // TODO: Should be injected
    private SongMetaManager songMetaManager;
    private Settings settings;
    private NonPersistentSettings nonPersistentSettings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        // TODO: Should be injected
        songMetaManager = SongMetaManager.Instance;
        settings = SettingsManager.Instance.Settings;
        nonPersistentSettings = SettingsManager.Instance.NonPersistentSettings;

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
        Task.Run(async () =>
        {
            try
            {
                await ScanPlaylistsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to scan playlists: {ex.Message}");
            }
        });
    }

    private async Task ScanPlaylistsAsync()
    {
        Debug.Log($"Scanning playlists on thread {Thread.CurrentThread.ManagedThreadId}");
        using DisposableStopwatch d = new("Scanning playlists took <ms> ms");

        playlists = new List<IPlaylist>();

        await ScanPlaylistsInFolderAsync(playlistFolder);

        List<string> songFolders = SettingsUtils.GetEnabledSongFolders(settings);
        foreach (string songFolder in songFolders)
        {
            await ScanPlaylistsInFolderAsync(songFolder);
        }
    }

    private async Task ScanPlaylistsInFolderAsync(string folder)
    {
        Debug.Log($"Scanning playlists in folder '{folder}'");
        using DisposableStopwatch d2 = new($"Scanning playlists in folder '{folder}' took <ms> ms");

        await ScanUltraStarPlaylistsInFolder(folder);
        await ScanM3UPlaylistsInFolder(folder);
    }

    private async Task ScanM3UPlaylistsInFolder(string folder)
    {
        FileScanner scanner = new($"*.{ApplicationUtils.m3uPlaylistFileExtension}", true, true);
        List<string> playlistFilePaths = scanner.GetFiles(folder, true);
        foreach (string filePath in playlistFilePaths)
        {
            try
            {
                M3UPlaylist playlist = M3UPlaylistParser.ParseFile(filePath);
                AddPlaylist(playlist, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to scan playlist '{filePath}': {ex.Message}");
            }
        }
    }

    private async Task ScanUltraStarPlaylistsInFolder(string folder)
    {
        string ultraStarPlaylistFileExtensionPattern = $"*.{ApplicationUtils.ultraStarPlaylistFileExtension}";
        FileScanner scanner = new(ultraStarPlaylistFileExtensionPattern, true, true);
        List<string> playlistFilePaths = scanner.GetFiles(folder, true);
        foreach (string filePath in playlistFilePaths)
        {
            try
            {
                UltraStarPlaylist playlist = UltraStarPlaylistParser.ParseFile(filePath);
                AddPlaylist(playlist, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to scan playlist '{filePath}': {ex.Message}");
            }
        }
    }

    private void AddPlaylist(IPlaylist playlist, string filePath)
    {
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

        HashSet<char> invalidCharacters = Path.GetInvalidPathChars()
            .Concat(new List<char> { '\\', '/' })
            .ToHashSet();
        if (invalidCharacters.AnyMatch(invalidChar => newName.Contains(invalidChar)))
        {
            return EPlaylistNameIssue.Invalid;
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

    public bool TrySetPlaylistName(IPlaylist playlist, string newName, out Translation errorMessage)
    {
        if (playlist == null
            || playlist.Name == newName)
        {
            errorMessage = Translation.Empty;
            return true;
        }

        UltraStarPlaylist ultraStarPlaylist = playlist as UltraStarPlaylist;
        if (playlist is UltraStarAllSongsPlaylist
            || playlist.Name == favoritesPlaylistName
            || playlist.FilePath.IsNullOrEmpty()
            || ultraStarPlaylist == null)
        {
            errorMessage = Translation.Get(R.Messages.playlist_error_cannotRename);
            return false;
        }

        if (GetPlaylistNameIssue(playlist, newName) != EPlaylistNameIssue.None)
        {
            errorMessage = Translation.Get(R.Messages.playlist_error_invalidName);
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
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to rename playlist to '{newName}': {ex.Message}");
            errorMessage = Translation.Get(R.Messages.common_errorWithReason, "reason", ex.Message);
            return false;
        }

        // Update settings
        if (nonPersistentSettings != null
            && nonPersistentSettings.PlaylistName.Value == oldName)
        {
            nonPersistentSettings.PlaylistName.Value = newName;
        }

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        errorMessage = Translation.Empty;
        return true;
    }

    public Translation TryRemovePlaylist(IPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist
            || playlist.Name == favoritesPlaylistName
            || playlist.FilePath.IsNullOrEmpty())
        {
            return Translation.Get(R.Messages.playlist_error_cannotRemove);
        }

        string oldName = playlist.Name;
        try
        {
            Debug.Log($"Deleting playlist '{oldName}'");
            File.Delete(playlist.FilePath);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to delete playlist '{oldName}': {ex.Message}");
            return Translation.Get(R.Messages.common_errorWithReason, "reason", ex.Message);
        }

        // Update settings
        if (nonPersistentSettings != null
            && nonPersistentSettings.PlaylistName.Value == oldName)
        {
            nonPersistentSettings.PlaylistName.Value = "";
        }

        playlists.Remove(playlist);

        playlistChangeEventStream.OnNext(new PlaylistChangeEvent(playlist, null));

        return Translation.Empty;
    }

    public UltraStarPlaylist CreateNewPlaylist(string initialName)
    {
        string newPlaylistName = GetNewUniquePlaylistName(initialName);
        string newPlaylistPath = $"{playlistFolder}/{newPlaylistName}.{ApplicationUtils.ultraStarPlaylistFileExtension}";

        // Create playlist file
        File.WriteAllText(newPlaylistPath, "# UltraStar playlist");
        FileUtils.SleepUntilFileExists(newPlaylistPath, 500);

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

    public bool HasPlaylist(string playlistName)
    {
        return GetPlaylistByName(playlistName) != null;
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
            return Translation.Get(R.Messages.playlistName_allSongs);
        }

        return playlist.Name;
    }
}
