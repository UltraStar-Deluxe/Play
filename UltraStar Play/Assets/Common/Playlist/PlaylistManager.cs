using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using System.IO;

public class PlaylistManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        playlistToFilePathMap.Clear();
        playlists.Clear();
        favoritesPlaylist = new UltraStarPlaylist();
    }

    public static PlaylistManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<PlaylistManager>("PlaylistManager");
        }
    }

    // static references to be persisted across scenes
    private static Dictionary<UltraStarPlaylist, string> playlistToFilePathMap = new Dictionary<UltraStarPlaylist, string>();
    private static List<UltraStarPlaylist> playlists = new List<UltraStarPlaylist>();
    private static UltraStarPlaylist favoritesPlaylist = new UltraStarPlaylist();

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
            return favoritesPlaylist;
        }
    }

    private readonly Subject<PlaylistChangeEvent> playlistChangeEventStream = new Subject<PlaylistChangeEvent>();
    public IObservable<PlaylistChangeEvent> PlaylistChangeEventStream => playlistChangeEventStream;

    private readonly string playlistFileExtension = ".playlist";
    private string favoritesPlaylistFile;
    private string playlistFolder;

    void Awake()
    {
        playlistFolder = Application.persistentDataPath + "/Playlists";
        favoritesPlaylistFile = playlistFolder + "/Favorites" + playlistFileExtension;

        CreateFavoritePlaylistIfNotExist();
    }

    private void CreateFavoritePlaylistIfNotExist()
    {
        if (!Directory.Exists(playlistFolder))
        {
            Directory.CreateDirectory(playlistFolder);
        }
        if (!File.Exists(favoritesPlaylistFile))
        {
            File.WriteAllText(favoritesPlaylistFile, "# UltraStar playlist");
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
            return favoritesPlaylistFile;
        }
        return playlistToFilePathMap[playlist];
    }

    private void ScanPlaylistFolder()
    {
        playlists = new List<UltraStarPlaylist>();
        playlistToFilePathMap = new Dictionary<UltraStarPlaylist, string>();

        FolderScanner scanner = new FolderScanner("*" + playlistFileExtension);
        List<string> playlistFilePaths = scanner.GetFiles(playlistFolder);
        foreach (string filePath in playlistFilePaths)
        {
            UltraStarPlaylist playlist = UltraStarPlaylistParser.ParseFile(filePath);
            playlists.Add(playlist);
            playlistToFilePathMap.Add(playlist, filePath);

            // This is the special playlist for the favorite songs.
            if (Path.GetFullPath(favoritesPlaylistFile) == Path.GetFullPath(filePath))
            {
                favoritesPlaylist = playlist;
            }
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
}
