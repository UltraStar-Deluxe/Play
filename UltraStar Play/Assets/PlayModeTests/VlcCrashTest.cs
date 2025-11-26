using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

[Ignore("Manual test")]
public class VlcCrashTest : AbstractPlayModeTest
{
    private const string SongAudioPlayerPrefabPath = "Assets/Common/MediaPlayer/SongAudioPlayer/SongAudioPlayer.prefab";
    private const string SongVideoPlayerPrefabPath = "Assets/Common/MediaPlayer/SongVideoPlayer/SongVideoPlayer.prefab";
    
    private const int MediaPlayerCount = 12;

    [Inject]
    private SongMetaManager songMetaManager;
    
    [Inject]
    private Injector injector;

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        settings.LogVlcOutput = true;
        settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        settings.SongDirs = new List<string>
        {
            "C:/Dev/UltraStar-Songs-Debug"
        };
    }

    protected override void ConfigureAndPrepareTestSongs(Settings settings)
    {
        // Use configured song folder
    }

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        // Use configured song folder
        return new List<string>();
    }

    [UnityTest]
    public IEnumerator ShouldPlayWithVlc() => ShouldPlayWithVlcAsync();
    public async Awaitable ShouldPlayWithVlcAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        List<SongMeta> songMetas = ScanSongs();
        
        for (int i = 0; i < MediaPlayerCount; i++)
        {
            SongAudioPlayer songAudioPlayer = CreateSongAudioPlayer();
            songAudioPlayer.gameObject.name = $"SongAudioPlayer {i}";
            SongVideoPlayer songVideoPlayer = CreateSongVideoPlayer(songAudioPlayer);
            songVideoPlayer.gameObject.name = $"SongVideoPlayer {i}";

            PlaySong(songAudioPlayer, songVideoPlayer, songMetas[i]);
        }

        await Awaitable.WaitForSecondsAsync(1);
    }

    private async Awaitable PlaySong(SongAudioPlayer songAudioPlayer, SongVideoPlayer songVideoPlayer, SongMeta songMeta)
    {
        double startPositionInMillis = Random.Range(0, 5000);
        
        Debug.Log($"Playing song. ArtistDashTitle: '{songMeta.GetArtistDashTitle()}', StartPosition: {startPositionInMillis}");
        
        await songAudioPlayer.LoadAndPlayAsync(songMeta, startPositionInMillis);
        Assert.IsTrue(songAudioPlayer.IsFullyLoaded, $"SongAudioPlayer is not fully loaded. Song: {songMeta.GetArtistDashTitle()}");
        Assert.IsTrue(songAudioPlayer.IsPlaying, $"SongAudioPlayer is not playing. Song: {songMeta.GetArtistDashTitle()}");

        if (!songMeta.Video.IsNullOrEmpty())
        {
            await songVideoPlayer.LoadAndPlayAsync(songMeta);
            Assert.IsTrue(songVideoPlayer.IsFullyLoaded, $"SongVideoPlayer is not fully loaded. Song: {songMeta.GetArtistDashTitle()}");
            Assert.IsTrue(songVideoPlayer.IsPlaying, $"SongVideoPlayer is not playing. Song: {songMeta.GetArtistDashTitle()}");
        }
    }

    private List<SongMeta> ScanSongs()
    {
        songMetaManager.ScanSongsIfNotDoneYet();
        songMetaManager.WaitUntilSongScanFinished();
        List<SongMeta> songMetas = songMetaManager.GetSongMetas()
            .OrderBy(songMeta => songMeta.GetArtistDashTitle())
            .ToList();
        Assert.Greater(songMetas.Count, MediaPlayerCount, "Not enough songs for test");
        return songMetas;
    }

    private SongAudioPlayer CreateSongAudioPlayer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongAudioPlayerPrefabPath);
        GameObject gameObject = Object.Instantiate(prefab);
        SongAudioPlayer songAudioPlayer = gameObject.GetComponent<SongAudioPlayer>();
        
        injector.InjectAllComponentsInChildren(songAudioPlayer);

        return songAudioPlayer;
    }
    
    private SongVideoPlayer CreateSongVideoPlayer(SongAudioPlayer songAudioPlayer)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongVideoPlayerPrefabPath);
        GameObject gameObject = Object.Instantiate(prefab);
        SongVideoPlayer songVideoPlayer = gameObject.GetComponent<SongVideoPlayer>();
        songVideoPlayer.songAudioPlayer = songAudioPlayer;
        
        injector
            .WithBindingForInstance(songAudioPlayer)
            .InjectAllComponentsInChildren(songVideoPlayer);

        return songVideoPlayer;
    }
}
