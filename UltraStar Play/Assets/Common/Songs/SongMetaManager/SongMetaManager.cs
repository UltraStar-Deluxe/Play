using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using Debug = UnityEngine.Debug;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : AbstractSingletonBehaviour
{
    public static SongMetaManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongMetaManager>();

    public int LoadedSongsCount => songMetaScanner.LoadedSongsCount;
    public double LoadedSongsPercent => songMetaScanner.LoadedSongsPercent;

    public bool IsSongScanFinished => songMetaScanner.IsSongScanFinished;
    public IObservable<SongScanFinishedEvent> SongScanFinishedEventStream => songMetaScanner.SongScanFinishedEventStream
        .ObserveOnMainThread();

    public IObservable<SongMeta> AddedSongMetaEventStream => songMetaCollection.AddedSongMetaEventStream
        .ObserveOnMainThread();

    public IObservable<SongMeta> RemovedSongMetaEventStream => songMetaCollection.RemovedSongMetaEventStream
        .ObserveOnMainThread();

    private readonly Subject<SongMeta> reloadedSongMetaEventStream = new();
    public IObservable<SongMeta> ReloadedSongMetaEventStream => reloadedSongMetaEventStream
        .ObserveOnMainThread();

    private readonly Subject<SongMeta> beforeSongMetaSavedEventStream = new();
    public IObservable<SongMeta> BeforeSongMetaSavedEventStream => beforeSongMetaSavedEventStream
        .ObserveOnMainThread();

    [InjectedInAwake]
    private Settings settings;

    private readonly SongMetaCollection songMetaCollection = new();
    private SongMetaScanner songMetaScanner;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        settings = SettingsManager.Instance.Settings;
        songMetaScanner = new(songMetaCollection, settings);
    }

    public SongMeta GetFirstSongMeta()
    {
        return GetSongMetas().FirstOrDefault();
    }

    public void AddSongMeta(SongMeta songMeta)
    {
        songMetaCollection.Add(songMeta);
    }

    public IReadOnlyCollection<SongMeta> GetSongMetas()
    {
        return songMetaCollection.SongMetas;
    }

    public void ScanSongsIfNotDoneYet()
    {
        songMetaScanner.ScanSongsIfNotDoneYet();
    }

    public void RescanSongs()
    {
        songMetaCollection.Clear();
        songMetaScanner.RescanSongs();
    }

    public void WaitUntilSongScanFinished()
    {
        songMetaScanner.WaitUntilSongScanFinished();
    }

    public static string GetAbsoluteGeneratedSongMetaFilePathForAudioFile(string generatedSongFolderAbsolutePath, string audioFile)
    {
        return ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, audioFile) + "/song-info.txt";
    }

    public void SaveSong(SongMeta songMeta, bool isAutoSave)
    {
        if (songMeta == null)
        {
            return;
        }

        beforeSongMetaSavedEventStream.OnNext(songMeta);

        SongIdManager.ClearSongIds(songMeta);

        CreateDirectory(songMeta);
        string songFilePath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
        try
        {
            // Write the song data structure to the file.
            UltraStarSongFormatVersion version = SettingsUtils.GetUltraStarSongFormatVersionForSave(settings, songMeta.Version);
            Debug.Log($"Saving song {songFilePath} using UltraStar format version {version.StringValue}");
            UltraStarFormatWriter.WriteFile(songFilePath, songMeta, version, settings.WriteUltraStarTxtFileWithByteOrderMark);

            // Update creation and modification time
            songMeta.FileInfo?.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_save,
                "reason", e.Message));
            return;
        }

        if (!isAutoSave)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_saveSuccess));
        }
    }

    public void RemoveSong(SongMeta songMeta)
    {
        songMetaCollection.RemoveUnsafe(songMeta);
    }

    public void ReloadSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        SongIdManager.ClearSongIds(songMeta);

        string absoluteFilePath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
        try
        {
            UltraStarSongParserResult parserResult = UltraStarSongParser.ParseFile(absoluteFilePath,
                new UltraStarSongParserConfig { Encoding = songMeta.FileEncoding, UseUniversalCharsetDetector = false });
            songMeta.CopyValues(parserResult.SongMeta);

            reloadedSongMetaEventStream.OnNext(songMeta);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to reload song {absoluteFilePath}: " + e.Message);
            Debug.LogException(e);
        }
    }

    public SongMeta GetSongMetaByTitle(string title)
    {
        return GetSongMetas().FirstOrDefault(songMeta => songMeta.Title == title);
    }

    public SongMeta GetSongMetaByTxtFilePath(string path)
    {
        if (path.IsNullOrEmpty())
        {
            return null;
        }

        return GetSongMetas().FirstOrDefault(songMeta => songMeta.FileInfo != null && songMeta.FileInfo.FullName == new FileInfo(path).FullName);
    }

    public SongMeta GetSongMetaByGloballyUniqueId(string songId)
    {
        if (songId.IsNullOrEmpty())
        {
            return null;
        }

        if (SongIdManager.TryGetSongMetaByGloballyUniqueId(songId, out SongMeta knownMatchingSongMeta))
        {
            return knownMatchingSongMeta;
        }

        SongMeta matchingSongMeta = GetSongMetas()
            .FirstOrDefault(songMeta => SongIdManager.SongMetaMatchesGloballyUniqueSongId(songMeta, songId));
        return matchingSongMeta;
    }

    public SongMeta GetSongMetaByLocallyUniqueId(string songId)
    {
        if (songId.IsNullOrEmpty())
        {
            return null;
        }

        if (SongIdManager.TryGetSongMetaByLocallyUniqueId(songId, out SongMeta knownMatchingSongMeta))
        {
            return knownMatchingSongMeta;
        }

        SongMeta matchingSongMeta = GetSongMetas()
            .FirstOrDefault(songMeta => SongIdManager.SongMetaMatchesLocallyUniqueSongId(songMeta, songId));
        return matchingSongMeta;
    }

    protected override void OnDestroySingleton()
    {
        songMetaScanner.CancelSongScan();
    }

    public bool ContainsSongMeta(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return false;
        }

        return GetSongMetas().Contains(songMeta);
    }

    private static void CreateDirectory(SongMeta songMeta)
    {
        DirectoryInfo directoryInfo = SongMetaUtils.GetDirectoryInfo(songMeta);
        if (directoryInfo == null
            || directoryInfo.Exists)
        {
            return;
        }

        directoryInfo.Create();
    }
}
