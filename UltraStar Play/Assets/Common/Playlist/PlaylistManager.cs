using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;

public class PlaylistManager : AbstractSingletonBehaviour, INeedInjection
{
    public static PlaylistManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<PlaylistManager>();

    private ConcurrentBag<IPlaylist> playlists = new();

    public IReadOnlyList<IPlaylist> Playlists
    {
        get
        {
            if (playlists.IsNullOrEmpty())
            {
                CreateFavoritePlaylistIfNotExist();
                ScanPlaylists();
            }
            return playlists.ToList();
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

    private readonly Subject<PlaylistChangedEvent> playlistChangedEventStream = new();
    public IObservable<PlaylistChangedEvent> PlaylistChangedEventStream => playlistChangedEventStream
        .ObserveOnMainThread();

    private readonly Subject<VoidEvent> playlistsLoadedEventStream = new();
    public IObservable<VoidEvent> PlaylistsLoadedEventStream => playlistsLoadedEventStream
        .ObserveOnMainThread();

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

        CreateFavoritePlaylistIfNotExist();
    }

    private void CreateFavoritePlaylistIfNotExist()
    {
        if (!Directory.Exists(ApplicationUtils.PlaylistFolder))
        {
            Directory.CreateDirectory(ApplicationUtils.PlaylistFolder);
        }
        if (!File.Exists(ApplicationUtils.FavoritesPlaylistFilePath))
        {
            File.WriteAllText(ApplicationUtils.FavoritesPlaylistFilePath, "# UltraStar playlist");
        }
    }

    public bool IsFavoritesPlaylist(IPlaylist playlist)
    {
        return playlist.Name == ApplicationUtils.FavoritesPlaylistName;
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

    private async void ScanPlaylists()
    {
        try
        {
            await ScanPlaylistsAsync();
            playlistsLoadedEventStream.OnNext(VoidEvent.instance);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to scan playlists: {ex.Message}");
        }
    }

    private async Awaitable ScanPlaylistsAsync()
    {
        await Awaitable.BackgroundThreadAsync();

        Debug.Log($"Scanning playlists on thread {Thread.CurrentThread.ManagedThreadId}");
        using DisposableStopwatch d = new("Scanning playlists took <ms> ms");

        playlists = new ConcurrentBag<IPlaylist>();

        await ScanPlaylistsInFolderAsync(ApplicationUtils.PlaylistFolder);

        List<string> songFolders = SettingsUtils.GetEnabledSongFolders(settings);
        foreach (string songFolder in songFolders)
        {
            await ScanPlaylistsInFolderAsync(songFolder);
        }

        await Awaitable.MainThreadAsync();
    }

    private async Awaitable ScanPlaylistsInFolderAsync(string folder)
    {
        Debug.Log($"Scanning playlists in folder '{folder}'");
        using DisposableStopwatch d2 = new($"Scanning playlists in folder '{folder}' took <ms> ms");

        await ScanUltraStarPlaylistsInFolderAsync(folder);
        await ScanM3UPlaylistsInFolderAsync(folder);
    }

    private async Awaitable ScanM3UPlaylistsInFolderAsync(string folder)
    {
        List<string> playlistFilePaths = FileScanner.GetFiles(folder,
            new FileScannerConfig($"*.{ApplicationUtils.M3uPlaylistFileExtension}") { Recursive = true });
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

    private async Awaitable ScanUltraStarPlaylistsInFolderAsync(string folder)
    {
        string ultraStarPlaylistFileExtensionPattern = $"*.{ApplicationUtils.UltraStarPlaylistFileExtension}";
        List<string> playlistFilePaths = FileScanner.GetFiles(folder, new FileScannerConfig(ultraStarPlaylistFileExtensionPattern) { Recursive = true });
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
            && Path.GetFullPath(ApplicationUtils.FavoritesPlaylistFilePath) == Path.GetFullPath(filePath))
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
        playlistChangedEventStream.OnNext(new PlaylistChangedEvent(playlist, songMeta));
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
        playlistChangedEventStream.OnNext(new PlaylistChangedEvent(playlist, songMeta));
        SavePlaylist(playlist);
    }

    public class PlaylistChangedEvent
    {
        public IPlaylist Playlist { get; set; }
        public SongMeta SongMeta { get; set; }

        public PlaylistChangedEvent(IPlaylist playlist, SongMeta songMeta)
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
            || playlist.Name == ApplicationUtils.FavoritesPlaylistName
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
        string newPath = $"{oldFolder}/{newName}.{ApplicationUtils.UltraStarPlaylistFileExtension}";
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

        playlistChangedEventStream.OnNext(new PlaylistChangedEvent(playlist, null));

        errorMessage = Translation.Empty;
        return true;
    }

    public Translation TryRemovePlaylist(IPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist
            || playlist.Name == ApplicationUtils.FavoritesPlaylistName
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

        playlists = new ConcurrentBag<IPlaylist>(playlists.Except(new List<IPlaylist> {playlist}));

        playlistChangedEventStream.OnNext(new PlaylistChangedEvent(playlist, null));

        return Translation.Empty;
    }

    public UltraStarPlaylist CreateNewPlaylist(string initialName)
    {
        string newPlaylistName = GetNewUniquePlaylistName(initialName);
        string newPlaylistPath = $"{ApplicationUtils.PlaylistFolder}/{newPlaylistName}.{ApplicationUtils.UltraStarPlaylistFileExtension}";

        // Create playlist file
        File.WriteAllText(newPlaylistPath, "# UltraStar playlist");
        FileUtils.SleepUntilFileExists(newPlaylistPath, 500);

        // Create playlist object
        UltraStarPlaylist newPlaylist = new(newPlaylistPath);
        AddPlaylist(newPlaylist, newPlaylistPath);

        playlistChangedEventStream.OnNext(new PlaylistChangedEvent(newPlaylist, null));

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
