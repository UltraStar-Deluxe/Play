using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SongMetaScanner
{
    private const int LazyLoadingSongRecommendationThresholdCount = 500;

    private readonly Subject<SongScanFinishedEvent> songScanFinishedEventStream = new();
    public IObservable<SongScanFinishedEvent> SongScanFinishedEventStream => songScanFinishedEventStream;

    private readonly Settings settings;
    private SongMetaCollection songMetaCollection;

    private bool isSongScanStarted;
    private bool isSongScanFinished;
    public bool IsSongScanFinished => isSongScanFinished;

    private int targetSongCount;
    public int LoadedSongsCount => songMetaCollection.Count;
    public double LoadedSongsPercent
    {
        get
        {
            if (targetSongCount <= 0)
            {
                return 0;
            }

            int result = 100 * songMetaCollection.Count / targetSongCount;
            if (result >= 100 && !isSongScanFinished)
            {
                return 99.9;
            }
            return result;
        }
    }

    private CancellationTokenSource songScanCancellationTokenSource;

    public SongMetaScanner(SongMetaCollection target, Settings settings)
    {
        this.settings = settings;
        this.songMetaCollection = target;
    }

    public void ScanSongsIfNotDoneYet()
    {
        if (isSongScanStarted)
        {
            return;
        }

        // Get translation once to trigger Resources.Load for translations
        // because it can only be called from the main thread and issues in songs are translated.
        Translation.Get(R.Messages.common_ok);

        // Update supported file formats when ffmpeg is (not) used.
        ApplicationUtils.UseFfmpegToPlayMediaFiles = settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never;
        ApplicationUtils.UseVlcToPlayMediaFiles = settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never;

        isSongScanStarted = true;
        isSongScanFinished = false;
        songScanCancellationTokenSource?.Cancel();
        songScanCancellationTokenSource = new();
        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);
        Task.Run(async () => await ScanSongsAsync(generatedSongFolderAbsolutePath, songScanCancellationTokenSource.Token));
    }

    public void RescanSongs()
    {
        CancelSongScan();

        targetSongCount = 0;
        isSongScanStarted = false;
        isSongScanFinished = false;

        ScanSongsIfNotDoneYet();
    }

    public void WaitUntilSongScanFinished()
    {
        ScanSongsIfNotDoneYet();
        float startTimeInSeconds = Time.time;
        float timeoutInSeconds = 2;
        while ((startTimeInSeconds + timeoutInSeconds) > Time.time)
        {
            if (isSongScanFinished)
            {
                return;
            }
            Thread.Sleep(100);
        }
        Debug.LogError("Song scan did not finish - timeout reached.");
    }

    private async Task ScanSongsAsync(string generatedSongFolderAbsolutePath, CancellationToken cancellationToken)
    {
        Debug.Log($"Started song scan on thread {Thread.CurrentThread.ManagedThreadId}");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            DirectoryUtils.CreateDirectory(generatedSongFolderAbsolutePath);

            // Find all txt and audio files in configured song folders and the generated song folder
            List<string> allSongFolders = SettingsUtils.GetEnabledSongFolders(settings)
                .Union(new List<string> { generatedSongFolderAbsolutePath })
                .ToList();

            await Task.WhenAll(allSongFolders
                .Select(songFolder => Task.Run(async () => await ScanFolderAsync(songFolder, cancellationToken))));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to scan songs: {ex.Message}");
        }
        finally
        {
            isSongScanFinished = true;
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms. Found {songMetaCollection.Count} songs.");
            songScanFinishedEventStream.OnNext(new SongScanFinishedEvent(songMetaCollection.Count));
        }
    }

    private async Task ScanFolderAsync(string folder, CancellationToken cancellationToken)
    {
        Debug.Log($"Scan songs in folder '{folder}' on thread {Thread.CurrentThread.ManagedThreadId}");

        await Task.WhenAll(
            ScanTxtFilesAsync(folder, cancellationToken),
            ScanMidiFilesAsync(folder, cancellationToken));
    }

    private async Task ScanTxtFilesAsync(string folder, CancellationToken cancellationToken)
    {
        List<string> txtFiles = FileScannerUtils.ScanForFiles(new List<string> { folder }, new List<string> { "*.txt" });
        cancellationToken.ThrowIfCancellationRequested();

        targetSongCount += txtFiles.Count;
        // Show notification to the user when switching to lazy loading of songs is recommended.
        if (targetSongCount > LazyLoadingSongRecommendationThresholdCount
            && settings.SongDataFetchType is EFetchType.Upfront)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_configureOnDemandSongLoading));
        }

        // Process batches of files in parallel
        int batchSize = settings.SongScanMaxBatchCount > 1
            ? Math.Max(100, txtFiles.Count / (settings.SongScanMaxBatchCount - 1))
            : txtFiles.Count;
        List<List<string>> txtFileBatches = SplitIntoSmallerLists(txtFiles, batchSize);
        await Task.WhenAll(txtFileBatches.Select(txtFileBatch =>
            Task.Run(async () => await LoadTxtFilesAsync(txtFileBatch, cancellationToken))));
    }

    private async Task ScanMidiFilesAsync(string folder, CancellationToken cancellationToken)
    {
        if (!settings.SearchMidiFilesWithLyrics)
        {
            return;
        }

        List<string> midiFiles = FileScannerUtils.ScanForFiles(new List<string> { folder }, GetMidiFileExtensionPatterns());
        await LoadMidiFilesAsync(midiFiles, cancellationToken);
    }

    private async Task LoadMidiFileAsync(string midiFile, CancellationToken cancellationToken)
    {
        Log.Verbose(() => $"Load '{Path.GetFileName(midiFile)}' on thread {Thread.CurrentThread.ManagedThreadId}");
        cancellationToken.ThrowIfCancellationRequested();

        if (!AudioFileMetaTagUtils.TryGetArtist(midiFile, out string artist))
        {
            artist = "";
        }
        if (!AudioFileMetaTagUtils.TryGetTitle(midiFile, out string title))
        {
            title = Path.GetFileNameWithoutExtension(midiFile);
        }

        float txtFileBpm = 300;
        Dictionary<EVoiceId, string> voiceIdToDisplayName = new();
        MidiFileSongMeta songMeta = new MidiFileSongMeta(
            artist,
            title,
            txtFileBpm,
            midiFile,
            voiceIdToDisplayName);
        songMetaCollection.Add(songMeta);
    }

    private List<string> GetMidiFileExtensionPatterns()
    {
        return ApplicationUtils.supportedMidiFiles
            .Select(fileExtension => $"*.{fileExtension}")
            .ToList();
    }

    private async Task LoadTxtFilesAsync(List<string> txtFiles, CancellationToken cancellationToken)
    {
        Log.Debug(() => $"Load {txtFiles.Count} txt files on thread {Thread.CurrentThread.ManagedThreadId}");
        cancellationToken.ThrowIfCancellationRequested();

        await Task.WhenAll(txtFiles
            .Select(txtFile => LoadTxtFileAsync(txtFile, cancellationToken)));
    }

    private async Task LoadMidiFilesAsync(List<string> midiFiles, CancellationToken cancellationToken)
    {
        Log.Debug(() => $"Load {midiFiles.Count} MIDI files on thread {Thread.CurrentThread.ManagedThreadId}");
        cancellationToken.ThrowIfCancellationRequested();

        await Task.WhenAll(midiFiles
            .Select(midiFile => LoadMidiFileAsync(midiFile, cancellationToken)));
    }

    private async Task LoadTxtFileAsync(string txtFile, CancellationToken cancellationToken)
    {
        Log.Verbose(() => $"Load '{Path.GetFileName(txtFile)}' on thread {Thread.CurrentThread.ManagedThreadId}");
        cancellationToken.ThrowIfCancellationRequested();

        string fileName = Path.GetFileName(txtFile);
        List<string> ignoredFileNames = new() { "license.txt" };
        if (ignoredFileNames.AnyMatch(ignoredFileName => string.Equals(fileName, ignoredFileName)))
        {
            return;
        }

        try
        {
            LazyLoadedFromFileSongMeta songMeta = new LazyLoadedFromFileSongMeta(txtFile);
            if (settings.SongDataFetchType is EFetchType.Upfront)
            {
                songMeta.LoadSongIfNotDoneYet();
            }

            songMetaCollection.Add(songMeta);
        }
        catch (UltraStarSongParserException e)
        {
            Debug.LogError($"{nameof(UltraStarSongParserException)}: " + txtFile + "\n" + e.Message);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load {txtFile}");
        }
    }

    public void CancelSongScan()
    {
        if (isSongScanStarted
            && !isSongScanFinished
            && songScanCancellationTokenSource != null)
        {
            Debug.Log($"Cancelling song scan");
            songScanCancellationTokenSource.Cancel();
        }
    }

    private static List<List<T>> SplitIntoSmallerLists<T>(List<T> source, int chunkSize)
    {
        // https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
        return source
            .Select((element, index) => new { Index = index, Value = element })
            .GroupBy(pair => pair.Index / chunkSize)
            .Select(grouping => grouping.Select(pair => pair.Value).ToList())
            .ToList();
    }
}
