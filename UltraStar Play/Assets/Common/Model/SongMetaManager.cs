using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : AbstractSingletonBehaviour
{
    private static readonly object scanLock = new();

    // The collection of songs is static to be persisted across scenes.
    // The collection is filled with song datas from a background thread, thus a thread-safe collection is used.
    private static ConcurrentBag<SongMeta> allSongMetas = new();
    private static ConcurrentBag<SongIssue> allSongIssues = new();
    private static List<SongIssue> SongErrors => allSongIssues.Where(songIssue => songIssue.Severity == ESongIssueSeverity.Error).ToList();
    private static List<SongIssue> SongWarnings => allSongIssues.Where(songIssue => songIssue.Severity == ESongIssueSeverity.Warning).ToList();

    private static Dictionary<string, List<string>> directoryToImageFiles = new();
    private static List<string> imageFileExtensionPatterns;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ResetSongMetas();
        lastSongDirs = null;
    }

    public static SongMetaManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongMetaManager>();

    // Static to be persisted across scenes.
    private static List<string> lastSongDirs;
    private static bool isSongScanStarted;
    private static bool isSongScanFinished;
    public static bool IsSongScanFinished
    {
        get
        {
            return isSongScanFinished;
        }
    }

    private readonly Subject<SongScanFinishedEvent> songScanFinishedEventStream = new();
    public IObservable<SongScanFinishedEvent> SongScanFinishedEventStream => songScanFinishedEventStream;

    private Settings settings;
    private Settings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = SettingsManager.Instance.Settings;
            }

            return settings;
        }
    }

    [Inject]
    private UiManager uiManager;
    
    public static void ResetSongMetas()
    {
        lock (scanLock)
        {
            allSongMetas = new ConcurrentBag<SongMeta>();
            allSongIssues = new ConcurrentBag<SongIssue>();
            isSongScanStarted = false;
            isSongScanFinished = false;
        }
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    public void ReloadSongMetas()
    {
        ResetSongMetas();
        ScanFilesIfNotDoneYet();
    }

    protected override void StartSingleton()
    {
        RescanIfSongFoldersChanged();
    }

    private void RescanIfSongFoldersChanged()
    {
        // Scene injection may not have finished here because DefaultSceneDataProviders may trigger a song scan.
        // Thus, use the static instance.
        if (lastSongDirs == null)
        {
            lastSongDirs = new List<string>(Settings.GameSettings.songDirs);
        }

        if (isSongScanFinished
            && !lastSongDirs.SequenceEqual(Settings.GameSettings.songDirs))
        {
            Debug.Log("SongDirs have changed since last scan. Start rescan.");
            lastSongDirs = new List<string>(Settings.GameSettings.songDirs);
            ResetSongMetas();
            ScanFilesIfNotDoneYet();
        }
    }

    private void AddSongMeta(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException(nameof(songMeta));
        }

        allSongMetas.Add(songMeta);
    }

    public SongMeta GetFirstSongMeta()
    {
        return GetSongMetas().FirstOrDefault();
    }

    public IReadOnlyCollection<SongMeta> GetSongMetas()
    {
        return allSongMetas;
    }

    public IReadOnlyList<SongIssue> GetSongIssues()
    {
        return allSongIssues.ToList();
    }

    public IReadOnlyList<SongIssue> GetSongErrors()
    {
        return SongErrors;
    }

    public IReadOnlyList<SongIssue> GetSongWarnings()
    {
        return SongWarnings;
    }

    public void ScanFilesIfNotDoneYet()
    {
        // First check. If the songs have been scanned already,
        // then this will quickly return and allows multiple threads access.
        if (!isSongScanStarted)
        {
            // The songs have not been scanned. Only one thread must perform the scan action.
            lock (scanLock)
            {
                // From here on, reading and writing the isInitialized flag can be considered atomic.
                // Second check. If multiple threads attempted to scan for songs (they passed the first check),
                // then only the first of these threads will start the scan.
                if (!isSongScanStarted)
                {
                    isSongScanStarted = true;
                    isSongScanFinished = false;
                    ScanFilesAsynchronously();
                }
            }
        }
    }

    private void ScanFilesAsynchronously()
    {
        Debug.Log("ScanFilesAsynchronously");

        // Scene injection may not have finished here because DefaultSceneDataProviders may trigger a song scan.
        // Thus, use the static instance.
        string generatedSongFolderAbsolutePath = ApplicationUtils.GetGeneratedSongFolderAbsolutePath();
        InitFolderIfNotDoneYet(generatedSongFolderAbsolutePath);
        
        List<string> txtFiles;
        List<string> audioFiles;
        lock (scanLock)
        {
            // Find all txt and audio files in configured song folders and the generated song folder
            List<string> allSongFolders = SettingsManager.Instance.Settings.GameSettings.songDirs
                .Union(new List<string> { generatedSongFolderAbsolutePath })
                .ToList();
            txtFiles = ScanForFiles(allSongFolders, new List<string> { "*.txt" });

            // Only search for audio files in configured song folders, not in the generated song folder
            audioFiles = ScanForFiles(SettingsManager.Instance.Settings.GameSettings.songDirs, GetAudioFileExtensionPatterns());
        }

        // Load the txt files in a background thread
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();

            Debug.Log("Started song-scan-thread.");
            lock (scanLock)
            {
                LoadSongMetasFromTxtFiles(txtFiles, out List<SongMeta> newSongMetas, out List<SongIssue> newSongIssues);
                allSongMetas.AddRange(newSongMetas);
                allSongIssues.AddRange(newSongIssues);
                isSongScanFinished = true;
            }
            stopwatch.Stop();
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms. Loaded {allSongMetas.Count} songs. Errors: {SongErrors.Count}, Warnings: {SongWarnings.Count}.");

            // Generate song meta for audio files that do not have a corresponding SongMeta.
            GenerateSongMetasForAudioFiles(generatedSongFolderAbsolutePath, audioFiles, allSongMetas.ToList());

            songScanFinishedEventStream.OnNext(new SongScanFinishedEvent());
        });
    }

    private void GenerateSongMetasForAudioFiles(string generatedSongFolderAbsolutePath, List<string> audioFiles, List<SongMeta> existingSongMetas)
    {
        if (!Settings.GameSettings.searchAudioFilesWithoutSongMeta)
        {
            return;
        }
        
        List<string> existingSongMetaAudioFiles = existingSongMetas
            .Select(songMeta => Path.GetFullPath(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3)))
            .ToList();

        List<string> audioFilesWithoutSongMeta = audioFiles
            .Where(audioFile => !ApplicationUtils.IsGeneratedAudioFile(audioFile))
            .Select(audioFile => Path.GetFullPath(audioFile))
            .Except(existingSongMetaAudioFiles)
            .ToList();

        Debug.Log($"Found {audioFilesWithoutSongMeta.Count} audio files without corresponding SongMeta");

        List<SongMeta> generatedSongMetas = audioFilesWithoutSongMeta
            .Select(audioFile => GenerateSongMetaForAudioFile(generatedSongFolderAbsolutePath, audioFile))
            .ToList();

        generatedSongMetas.ForEach(songMeta => allSongMetas.Add(songMeta));
    }

    private SongMeta GenerateSongMetaForAudioFile(string generatedSongFolderAbsolutePath, string audioFile)
    {
        if (!AudioFileMetaTagUtils.TryGetArtist(audioFile, out string artist))
        {
            artist = "";
        }
        if (!AudioFileMetaTagUtils.TryGetTitle(audioFile, out string title))
        {
            title = Path.GetFileNameWithoutExtension(audioFile);
        }

        // TODO: use https://github.com/WestHillApps/UniBpmAnalyzer to analyze bpm
        // TODO: use https://github.com/Zeugma440/atldotnet to read meta tags.
        float bpm = 300;

        string audioFileDirectory = Path.GetDirectoryName(audioFile);
        string absoluteSongMetaFilePath = GetAbsoluteGeneratedSongMetaFilePathForAudioFile(generatedSongFolderAbsolutePath, audioFile);
        string songMetaFileName = Path.GetFileName(absoluteSongMetaFilePath);
        string songMetaDirectory = Path.GetDirectoryName(absoluteSongMetaFilePath);
        Dictionary<string, string> voiceNames = new();
        SongMeta songMeta = new(songMetaDirectory, songMetaFileName, "", artist, bpm, audioFile, title, voiceNames, Encoding.UTF8);

        if (songMeta.Cover.IsNullOrEmpty()
            && TryFindCoverImageInSameFolder(audioFileDirectory, out string coverImage))
        {
            songMeta.Cover = coverImage;
        }
        
        if (songMeta.Background.IsNullOrEmpty()
            && TryFindBackgroundImageInSameFolder(audioFileDirectory, out string backgroundImage))
        {
            songMeta.Background = backgroundImage;
        }
        
        // Load lyrics and notes from MIDI file
        string fileExtension = Path.GetExtension(new Uri(audioFile).LocalPath);
        if (ApplicationUtils.IsSupportedMidiFormat(fileExtension))
        {
            songMeta.onPostProcessLoadedVoices = () => MidiToSongMetaUtils.FillSongMetaWithMidiLyricsAndNotes(songMeta);
        }
        
        Debug.Log("Generated SongMeta: " + songMeta);
        return songMeta;
    }

    public static string GetAbsoluteGeneratedSongMetaFilePathForAudioFile(string generatedSongFolderAbsolutePath, string audioFile)
    {
        return ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, audioFile) + "/song-info.txt";
    }

    private List<string> GetAudioFileExtensionPatterns()
    {
        return ApplicationUtils.supportedAudioFiles
            .Select(fileExtension => $"*.{fileExtension}")
            .ToList();
    }

    private void InitFolderIfNotDoneYet(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.Log("Creating folder: " + path);
            Directory.CreateDirectory(path);
        }
    }

    private void LoadSongMetasFromTxtFiles(List<string> txtFiles, out List<SongMeta> songMetas, out List<SongIssue> songIssues)
    {
        songMetas = new();
        songIssues = new();
        foreach (string path in txtFiles)
        {
            if (TryLoadSongMetaFromFile(path, out SongMeta newSongMeta, out List<SongIssue> newSongIssues))
            {
                songMetas.Add(newSongMeta);
            }
            songIssues.AddRange(newSongIssues);
        }
    }

    private static List<string> ScanForFiles(List<string> folders, List<string> fileExtensionPatterns)
    {
        FolderScanner folderScanner = new(fileExtensionPatterns);
        List<string> files = new();
        foreach (string songDir in folders)
        {
            try
            {
                List<string> txtFilesInSongDir = folderScanner.GetFiles(songDir, true);
                files.AddRange(txtFilesInSongDir);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        Debug.Log($"Found {files.Count} files matching pattern {fileExtensionPatterns.ToCsv()} in folders: {folders.ToCsv()}");
        return files;
    }

    public void WaitUntilSongScanFinished()
    {
        ScanFilesIfNotDoneYet();
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

    public bool TryLoadAndAddSongMetasFromFolder(string songFolder, out List<SongMeta> songMetas, out List<SongIssue> songIssues)
    {
        songMetas = new List<SongMeta>();
        songIssues = new List<SongIssue>();
        if (!Directory.Exists(songFolder))
        {
            return false;
        }

        FolderScanner txtScanner = new("*.txt");
        List<string> txtFiles = txtScanner.GetFiles(songFolder, true);

        LoadSongMetasFromTxtFiles(txtFiles, out List<SongMeta> newSongMetas, out List<SongIssue> newSongIssues);
        allSongMetas.AddRange(newSongMetas);
        allSongIssues.AddRange(newSongIssues);

        songMetas.AddRange(newSongMetas);
        newSongIssues.AddRange(newSongIssues);

        return true;
    }

    private bool TryLoadSongMetaFromFile(string path, out SongMeta songMeta, out List<SongIssue> songIssues)
    {
        songIssues = new List<SongIssue>();
        try
        {
            SongMeta newSongMeta = SongMetaBuilder.ParseFile(path, out List<SongIssue> parseFileIssues, null, Settings.DeveloperSettings.useUniversalCharsetDetector);
            songIssues.AddRange(parseFileIssues);

            List<SongIssue> mediaFormatIssues = SongMetaUtils.GetSupportedMediaFormatIssues(newSongMeta);
            songIssues.AddRange(mediaFormatIssues);

            if (songIssues.AllMatch(songIssue => songIssue.Severity == ESongIssueSeverity.Warning))
            {
                // No issues or only warnings, thus ok.
                
                if (newSongMeta.Cover.IsNullOrEmpty()
                    && TryFindCoverImageInSameFolder(newSongMeta.Directory, out string coverImage))
                {
                    newSongMeta.Cover = Path.GetFileName(coverImage);
                }
                
                if (newSongMeta.Background.IsNullOrEmpty()
                    && TryFindCoverImageInSameFolder(newSongMeta.Directory, out string backgroundImage))
                {
                    newSongMeta.Background = Path.GetFileName(backgroundImage);
                }
                
                songMeta = newSongMeta;
                return true;
            }
        }
        catch (SongMetaBuilderException e)
        {
            Debug.LogError("SongMetaBuilderException: " + path + "\n" + e.Message);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load {path}");
        }

        songMeta = null;
        return false;
    }

    private bool TryFindImagesFiles(string folder, out List<string> imageFiles)
    {
        if (!DirectoryUtils.Exists(folder))
        {
            imageFiles = null;
            return false;
        }

        if (!directoryToImageFiles.TryGetValue(folder, out imageFiles))
        {
            imageFiles = ScanForImageFiles(new List<string>() { folder });
            directoryToImageFiles[folder] = imageFiles;
        }

        return !imageFiles.IsNullOrEmpty();
    }
    
    private bool TryFindCoverImageInSameFolder(string folder, out string imageFile)
    {
        if (!TryFindImagesFiles(folder, out List<string> imageFiles))
        {
            imageFile = "";
            return false;
        }
        
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "cover", "front", "album", "co" };
        imageFile = imageFiles
            .FirstOrDefault(imageFile => searchTerms.AnyMatch(searchTerm =>
                imageFile.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .OrIfNull(imageFiles.FirstOrDefault());
        return true;
    }
    
    private bool TryFindBackgroundImageInSameFolder(string folder, out string imageFile)
    {
        if (!TryFindImagesFiles(folder, out List<string> imageFiles))
        {
            imageFile = "";
            return false;
        }
        
        // Prefer image files that have "background" or similar in their name.
        List<string> searchTerms = new List<string>() { "background", "back", "bg" };
        imageFile = imageFiles
            .FirstOrDefault(imageFile => searchTerms.AnyMatch(searchTerm =>
                imageFile.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .OrIfNull(imageFiles.FirstOrDefault());
        return true;
    }

    private List<string> ScanForImageFiles(List<string> folders)
    {
        if (imageFileExtensionPatterns == null)
        {
            imageFileExtensionPatterns = ApplicationUtils.supportedImageFiles
                .Select(fileExtension => "*." + fileExtension)
                .ToList();
        }
        
        return ScanForFiles(folders, imageFileExtensionPatterns);
    }

    public void SaveSong(SongMeta songMeta, bool isAutoSave)
    {
        SongMetaUtils.CreateDirectory(songMeta);
        string songFilePath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
        try
        {
            // Write the song data structure to the file.
            UltraStarSongFileWriter.WriteFile(songFilePath, songMeta);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            UiManager.CreateNotification("Saving the file failed:\n" + e.Message);
            return;
        }

        if (!isAutoSave)
        {
            UiManager.CreateNotification("Saved file");
        }
    }

    public void ReloadSong(SongMeta songMeta)
    {
        string absoluteFilePath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
        try
        {
            SongMeta other = SongMetaBuilder.ParseFile(absoluteFilePath, out List<SongIssue> _, songMeta.Encoding, false);
            songMeta.CopyValues(other);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to reload song {absoluteFilePath}: " + e.Message);
            Debug.LogException(e);
        }
    }

    public SongMeta GetSongMetaById(string songId)
    {
        if (songId.IsNullOrEmpty())
        {
            return null;
        }
        SongMeta matchingSongMeta = allSongMetas.FirstOrDefault(songMeta => songMeta.SongHash == songId);
        return matchingSongMeta;
    }
    
    public List<SongMeta> GetSongMetasByIds(List<string> songIds)
    {
        if (songIds.IsNullOrEmpty())
        {
            return new();
        }
        
        return songIds
            .Select(id => GetSongMetaById(id))
            .ToList();
    }
}
