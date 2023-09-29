using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using FullSerializer;
using ICSharpCode.SharpZipLib.Zip;
using UniInject;
using UniRx;
using UnityEngine;

public class UsdbAnimuxDeSongRepository : IOnLoadMod, ISongRepository, ISceneMod
{
    private static readonly string PhpSessionIdCookieName = "PHPSESSID";

    private static readonly string songListRowRegexPattern = @"<td onclick=""show_detail\((\d+)\)"">(.*)</td>\n" +
                                                             @"<td onclick=""show_detail\(\d+\)""><a href=.*>(.*)</td>\n" +
                                                             @"<td onclick=""show_detail\(\d+\)"">(.*)</td>\n" +
                                                             @"<td onclick=""show_detail\(\d+\)"">(.*)</td>\n" +
                                                             @"<td onclick=""show_detail\(\d+\)"">(.*)</td>\n" +
                                                             @"<td onclick=""show_detail\(\d+\)"">(.*)</td>\n" +
                                                             @"<td onclick=""show_detail\(\d+\)"">(.*)</td>";
    private static readonly Regex songListRowRegex = new Regex(songListRowRegexPattern, RegexOptions.Multiline);
    private static readonly Regex youTubeVideoIdRegex = new Regex(@"v=(\w+)(\r|\n|\,)", RegexOptions.Multiline);
    private static readonly Regex youTubeHtmlAnchorElementRegex = new Regex(@"<a .+ title=""(.+)"" /watch\?v=(\w+)(\r|\n|\,)", RegexOptions.Multiline);

    private static Dictionary<int, SongMeta> usdbSongIdToSongMeta = new Dictionary<int, SongMeta>();
    private static Dictionary<string, List<SongRepositorySearchResultEntry>> searchTermToSearchResult = new Dictionary<string, List<SongRepositorySearchResultEntry>>();

    private const int MaxSongId = 30_000;
    // private const int MaxSongId = 1000;

    private const int MaxSongsPerPage = 100;
    private const int MaxSongsPerSearchResult = 15;

    private const int DebugOnlyMaxSongsToLoadFromSongIndex = -100;

    [Inject]
    private UsdbAnimuxDeSongRepositoryModSettings modSettings;

    [Inject]
    private ModObjectContext modObjectContext;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private SongMetaManager songMetaManager;

    private UsdbSongIndex songIndex = new UsdbSongIndex();
    private string PersistedSongIndexFilePath => $"{modObjectContext.ModPersistentDataFolder}/song-index.json";

    private FlurlCookie sessionCookie;

    private int fetchingSongDetailsSemaphore;

    private string lastUsername;
    private string lastPassword;

    private bool hasLoadedFullSongsIndex;

    public void OnLoadMod()
    {
        LoadFullSongIndexIfEnabled();
    }

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        LoadFullSongIndexIfEnabled();
    }

    private void LoadFullSongIndexIfEnabled()
    {
        if (!modSettings.loadFullSongIndex)
        {
            return;
        }

        if (modSettings.username.IsNullOrEmpty()
            && modSettings.password.IsNullOrEmpty())
        {
            Debug.Log("Not loading song index from usdb.animux.de because credentials are empty.");
            return;
        }

        if (hasLoadedFullSongsIndex)
        {
            Debug.Log("Not loading song index from usdb.animux.de because it has been loaded already.");
            return;
        }

        if (lastUsername == modSettings.username
            && lastPassword == modSettings.password)
        {
            // Credentials did not change, ignore.
            Debug.Log("Not loading song index from usdb.animux.de because credentials did not change since last time.");
            return;
        }
        lastUsername = modSettings.username;
        lastPassword = modSettings.password;

        Task.Run(async () =>
        {
            try
            {
                await LoadFullSongIndexAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load song index from usdb.animux.de: {ex.Message}");
            }
        });
    }

    private async Task LoadFullSongIndexAsync()
    {
        await SynchronizeSongIndexAsync();

        hasLoadedFullSongsIndex = true;

        await AddSongMetasForSongIndexAsync();

        if (modSettings.loadFullSongIndexDetails)
        {
            await LoadAllSongIndexSongDetailsAsync();
        }
    }

    private async Task LoadAllSongIndexSongDetailsAsync()
    {
        Debug.Log($"Loading song details of {songIndex.Count} songs in song index.");

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Job job = new Job("Downloading song details from usdb.animux.de");
        job.OnCancel = () => cancellationTokenSource.Cancel();
        job.EstimatedTotalDurationInMillis = songIndex.Count * 10;
        job.SetStatus(EJobStatus.Running);
        jobManager.AddJob(job);

        List<int> usdbSongIds = songIndex.usdbSongIdToUsdbSong
            .Keys
            .OrderBy(id => id)
            .ToList();
        for (int i = 0; i < songIndex.Count; i++)
        {
            job.EstimatedCurrentProgressInPercent = 100 * ((double)i / songIndex.Count);
            if (cancellationTokenSource.IsCancellationRequested)
            {
                Debug.Log($"Cancel requested when loading song details {i} of {songIndex.Count}");
                break;
            }

            int usdbSongId = usdbSongIds[i];
            UsdbSong usdbSong = songIndex.usdbSongIdToUsdbSong[usdbSongId];
            try
            {
                await GetSongDetailsAsync(usdbSong);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load song details of '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId}): {ex.Message}");
            }
        }

        job.SetResult(EJobResult.Ok);
    }

    private async Task SynchronizeSongIndexAsync()
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Job job = new Job("Updating song index from usdb.animux.de");
        job.OnCancel = () => cancellationTokenSource.Cancel();
        job.EstimatedTotalDurationInMillis = MaxSongId * 10;
        job.SetStatus(EJobStatus.Running);
        jobManager.AddJob(job);

        try
        {
            await UpdateSongIndexAsync(job, cancellationTokenSource.Token);
            Debug.Log($"Successfully updated song index from usdb.animux.de");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            string errorMessage = $"Failed to update song index from usdb.animux.de: {ex.Message}";
            Debug.LogError(errorMessage);
            job.SetResult(EJobResult.Error);

            throw new Exception(errorMessage);
        }

        job.SetResult(EJobResult.Ok);
    }

    private async Task AddSongMetasForSongIndexAsync()
    {
        List<UsdbSong> usdbSongs = songIndex.usdbSongIdToUsdbSong
            .Values
            .ToList();
        
        for (int i = 0; i < usdbSongs.Count; i++)
        {
            if (DebugOnlyMaxSongsToLoadFromSongIndex > 0
                && i > DebugOnlyMaxSongsToLoadFromSongIndex)
            {
                break;
            }

            UsdbSong usdbSong = usdbSongs[i];

            try
            {
                SongMeta songMeta = CreateSongMetaFromUsdbSong(usdbSong);
                songMetaManager.AddSongMeta(songMeta);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to add SongMeta for usdb.animux.de song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId})");
            }
        }
    }

    private SongMeta CreateSongMetaFromUsdbSong(UsdbSong usdbSong)
    {
        if (usdbSongIdToSongMeta.TryGetValue(usdbSong.songId, out SongMeta cachedSongMeta))
        {
            return cachedSongMeta;
        }

        UsdbUltraStarSongMeta songMeta = new UsdbUltraStarSongMeta(
            usdbSong.artist,
            usdbSong.title);

        songMeta.OnLoadDetails = () => Task.Run(async () =>
        {
            // Sadly, querying multiple song details at once is not possible
            // because the "ziparchiv" data needs to be set via a cookie, which is stored on server side.
            // Thus, make an attempt at avoiding overlaps in this critical section. But no guarantees.
            long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
            long maxWaitTimeInMillis = 30000;
            while (fetchingSongDetailsSemaphore > 0)
            {
                if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis))
                {
                    throw new Exception($"Failed to get song details of '{SongMetaUtils.GetArtistDashTitle(songMeta)}' after waiting {maxWaitTimeInMillis} ms");
                }

                Debug.Log($"Waiting for other song details to be fetched before fetching details of '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");
                ThreadUtils.Sleep(1000);
            }

            try
            {
                fetchingSongDetailsSemaphore++;
                await LoadSongMetaDetailsAsync(songMeta, usdbSong);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load details of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId}) from usdb.animux.de: {ex.Message}");
            }
            finally
            {
                fetchingSongDetailsSemaphore--;
            }
        });

        usdbSongIdToSongMeta[usdbSong.songId] = songMeta;

        return songMeta;
    }

    private async Task<UsdbSongIndex> UpdateSongIndexAsync(Job job, CancellationToken cancellationToken)
    {
        if (modSettings.maxSongIndexCacheAgeInDays > 0
            && FileUtils.Exists(PersistedSongIndexFilePath))
        {
            Debug.Log($"Loading song index from cache file '{PersistedSongIndexFilePath}'");
            LoadSongIndexFromCacheFile();

            if (songIndex.synchronizationDateTime.AddDays(modSettings.maxSongIndexCacheAgeInDays) < DateTime.Now)
            {
                Debug.Log($"Updating song index from usdb.animux.de because cached song index is older than {modSettings.maxSongIndexCacheAgeInDays} days.");
            }
            else
            {
                Debug.Log($"Using song index from cache because the cache is younger than {modSettings.maxSongIndexCacheAgeInDays} days.");
                return songIndex;
            }
        }

        if (sessionCookie == null)
        {
            sessionCookie = await GetNewSessionCookieAsync();
        }
        List<UsdbSong> availableSongs = await GetAvailableSongs(job, cancellationToken, sessionCookie);
        Debug.Log($"Found {availableSongs.Count} songs on usdb.animux.de");

        songIndex.AddSongs(availableSongs);
        songIndex.synchronizationDateTime = DateTime.Now;

        SaveSongIndexToCacheFile();

        return songIndex;
    }

    private void LoadSongIndexFromCacheFile()
    {
        string loadedJson = File.ReadAllText(PersistedSongIndexFilePath, Encoding.UTF8);
        PersistedUsdbSongIndex loadedPersistedSongIndex = JsonConverter.FromJson<PersistedUsdbSongIndex>(loadedJson);
        UsdbSongIndex loadedSongIndex = PersistedUsdbSongIndex.FromPersistedSongIndex(loadedPersistedSongIndex);
        songIndex = loadedSongIndex;
        Debug.Log($"Successfully loaded song index from cache with {songIndex.Count} songs from '{PersistedSongIndexFilePath}'.");
    }

    private void SaveSongIndexToCacheFile()
    {
        PersistedUsdbSongIndex persistedUsdbSongIndex = PersistedUsdbSongIndex.FromSongIndex(songIndex);
        string json = JsonConverter.ToJson(persistedUsdbSongIndex);
        Debug.Log($"Saving song index to cache '{PersistedSongIndexFilePath}'");
        File.WriteAllText(PersistedSongIndexFilePath, json, Encoding.UTF8);
        Debug.Log($"Successfully saved song index with {songIndex.Count} songs to cache '{PersistedSongIndexFilePath}'.");
    }

    private async Task<List<UsdbSong>> GetAvailableSongs(Job job, CancellationToken cancellationToken, FlurlCookie sessionCookie)
    {
        // Code ported to C# from https://github.com/bohning/usdb_syncer/blob/main/src/usdb_syncer/usdb_scraper.py
        List<UsdbSong> availableSongs = new List<UsdbSong>();
        Dictionary<string, string> payload = new Dictionary<string, string>()
        {
            { "order", "id"},
            { "ud", "desc"},
            { "limit", MaxSongsPerPage.ToString() },
        };

        for (int start = 0; start < MaxSongId; start += MaxSongsPerPage)
        {
            job.EstimatedCurrentProgressInPercent = 100 * ((double)start / MaxSongId);
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"Fetching song index from {start} to {start + MaxSongsPerPage} from usdb.animux.de");

            payload["start"] = start.ToString();
            string html = await "https://usdb.animux.de/index.php"
                .SetQueryParam("link", "list")
                .WithCookie(sessionCookie.Name, sessionCookie.Value)
                .PostUrlEncodedAsync(payload)
                .ReceiveString();

            List<UsdbSong> newSongs = CreateUsdbSongsFromHtml(html);

            availableSongs.AddRange(newSongs);

            if (newSongs.Count < MaxSongsPerPage)
            {
                // No more songs left on the website
                break;
            }
        }

        return availableSongs;
    }

    private async Task<List<UsdbSong>> SearchSongsAsync(
        FlurlCookie sessionCookie,
        string artist,
        string title)
    {
        Debug.Log($"Searching songs on usdb.animux.de matching artist '{artist}' AND title '{title}'");

        Dictionary<string, string> payload = new Dictionary<string, string>()
        {
            { "interpret", artist},
            { "title", title},
            { "order", "id"},
            { "ud", "desc"},
            { "limit", MaxSongsPerSearchResult.ToString() },
        };
        string html = await "https://usdb.animux.de/index.php"
            .SetQueryParam("link", "list")
            .WithCookie(sessionCookie.Name, sessionCookie.Value)
            .PostUrlEncodedAsync(payload)
            .ReceiveString();

        List<UsdbSong> songs = CreateUsdbSongsFromHtml(html);

        Debug.Log($"Found {songs.Count} songs on usdb.animux.de matching artist '{artist}' AND title '{title}'");

        return songs;
    }

    private async Task LoadSongMetaDetailsAsync(UltraStarSongMeta songMeta, UsdbSong usdbSong)
    {
        UsdbSongDetails usdbSongDetails = await GetSongDetailsAsync(usdbSong);
        if (usdbSongDetails == null
            || usdbSongDetails.txtContent.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to get UltraStar txt content of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId})");
            songMeta.AddVoice(new Voice(EVoiceId.P1));
            return;
        }

        UltraStarSongMeta parsedSongMeta = UltraStarSongParser.ParseString(usdbSongDetails.txtContent, out List<SongIssue> songIssues);
        parsedSongMeta.Cover = usdbSongDetails.coverImage;
        parsedSongMeta.Background = usdbSongDetails.backgroundImage;
        if (FileUtils.Exists(usdbSongDetails.txtFile))
        {
            parsedSongMeta.SetFileInfo(usdbSongDetails.txtFile);
        }

        songMeta.CopyValues(parsedSongMeta);
        songMeta.RemoteSource = "usdb.animux.de";

        if (songMeta.Website.IsNullOrEmpty())
        {
            // Try to find link to YouTube video
            string youTubeVideoId;
            Match match = youTubeVideoIdRegex.Match(usdbSongDetails.txtContent);
            if (match.Success)
            {
                youTubeVideoId = match.Groups[1].Value;
            }
            else
            {
                youTubeVideoId = await SearchYouTubeVideoIdAsync(songMeta);
            }

            if (!youTubeVideoId.IsNullOrEmpty())
            {
                songMeta.Website = $"https://www.youtube.com/watch?v={youTubeVideoId}";
            }
            else
            {
                Debug.Log($"Failed to find YouTube video for song '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");
            }
        }

        // Voices are not yet copied because these are lazy loaded from txt content.
        parsedSongMeta.Voices.ForEach(voice => songMeta.AddVoice(voice));
    }

    private async Task<string> SearchYouTubeVideoIdAsync(SongMeta songMeta)
    {
        // TODO: Could be implemented via YouTube search API
        return "";
    }

    private async Task<UsdbSongDetails> GetSongDetailsAsync(UsdbSong usdbSong)
    {
        string detailsFolder = GetSongDetailsFolder(usdbSong);

        // Try to use previously downloaded txt file.
        if (DirectoryUtils.Exists(detailsFolder))
        {
            List<string> folders = new List<string>()
            {
                detailsFolder
            };

            List<string> txtFileExtensionPatterns = new List<string>()
            {
                "*.txt",
            };

            string txtFile = FileScannerUtils.ScanForFiles(folders, txtFileExtensionPatterns)
                .FirstOrDefault();
            if (FileUtils.Exists(txtFile))
            {
                SearchCoverAndBackgroundImageInFolder(detailsFolder, out string coverImage, out string backgroundImage);
                string txtContent = ReadPlainTextFileWithUnknownEncoding(txtFile);
                return new UsdbSongDetails()
                {
                    txtFile = txtFile,
                    txtContent = txtContent,
                    coverImage = coverImage,
                    backgroundImage = backgroundImage,
                };
            }
        }

        UsdbSongDetails details = await GetSongDetailsFromRemoteAsync(usdbSong);
        return details;
    }

    private async Task<UsdbSongDetails> GetSongDetailsFromRemoteAsync(UsdbSong usdbSong)
    {
        if (sessionCookie == null)
        {
            sessionCookie = await GetNewSessionCookieAsync();
        }

        Debug.Log($"Downloading details of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId})");

        // Song IDs are separated by | (pipe) in a cookie named "ziparchiv".
        // These have to be send first to a URI with link=ziparchiv,
        // and afterwards to a URI with link=ziparchiv&save=1.
        // Afterwards, the archive with selected songs can be downloaded from a separate URI.
        IFlurlResponse setZipArchiveSongIdsResponse = await $"https://usdb.animux.de/"
            .SetQueryParam("link", "ziparchiv")
            .WithCookie(sessionCookie.Name, sessionCookie.Value)
            .WithCookie("counter", $"1")
            .WithCookie("ziparchiv", $"{usdbSong.songId}|")
            .GetAsync();
        Debug.Log($"Received status code {setZipArchiveSongIdsResponse.StatusCode} when setting ZIP archive song ids to {usdbSong.songId}");

        IFlurlResponse setZipArchiveSongIdsSaveResponse = await $"https://usdb.animux.de/"
            .SetQueryParam("link", "ziparchiv")
            .SetQueryParam("save", "1")
            .WithCookie(sessionCookie.Name, sessionCookie.Value)
            .WithCookie("counter", $"1")
            .WithCookie("ziparchiv", $"{usdbSong.songId}|")
            .GetAsync();
        Debug.Log($"Received status code {setZipArchiveSongIdsSaveResponse.StatusCode} when setting ZIP archive song ids to {usdbSong.songId} with save=1");

        // Download last specified ZIP archive
        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        IFlurlResponse zipArchiveResponse = await $"https://usdb.animux.de/data/downloads/{modSettings.username}'s%20Playlist.zip"
            .SetQueryParam("t", currentTimeInMillis)
            .WithCookie(sessionCookie.Name, sessionCookie.Value)
            .WithCookie("counter", $"")
            .WithCookie("ziparchiv", $"")
            .WithHeader("User-Agent", "Some User Agent")
            .GetAsync();

        Debug.Log($"Received status code {zipArchiveResponse.StatusCode} when fetching details of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId})");

        byte[] zipArchiveBytes = await zipArchiveResponse.GetBytesAsync();
        Debug.Log($"Received {zipArchiveBytes.Length} bytes as details of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId})");

        if (zipArchiveBytes.Length <= 0)
        {
            throw new Exception("Empty response when fetching song details as archive");
        }

        UsdbSongDetails details = GetSongDetailsFromZipArchiveByteArray(usdbSong, zipArchiveBytes);
        return details;
    }

    private UsdbSongDetails GetSongDetailsFromZipArchiveByteArray(UsdbSong usdbSong, byte[] zipArchiveBytes)
    {
        string extractPath = GetSongDetailsFolder(usdbSong);
        Debug.Log($"Extracting ZIP archive with details of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId}) to folder '{extractPath}'");

        List<string> extractedFiles = new List<string>();

        using (MemoryStream ms = new MemoryStream(zipArchiveBytes))
        {
            using (ZipInputStream zipInputStream = new ZipInputStream(ms))
            {
                ZipEntry entry;
                while ((entry = zipInputStream.GetNextEntry()) != null)
                {
                    if (!entry.IsFile)
                    {
                        Debug.Log($"Skipping ZIP entry {entry.Name}");
                        continue;
                    }

                    Debug.Log($"Extracting ZIP entry {entry.Name}");

                    string extractedFileName = PathUtils.ReplaceInvalidFileNameChars(Path.GetFileName(entry.Name));
                    string extractedFilePath = $"{extractPath}/{extractedFileName}";

                    // Create directory structure if it doesn't exist
                    DirectoryUtils.CreateDirectory(Path.GetDirectoryName(extractedFilePath));

                    using (FileStream entryFileStream = File.Create(extractedFilePath))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = zipInputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            entryFileStream.Write(buffer, 0, bytesRead);
                        }
                    }

                    extractedFiles.Add(extractedFilePath);
                }
            }
        }

        Debug.Log($"Finished extracting ZIP archive with details of song '{usdbSong.artist} - {usdbSong.title}' (usdb id {usdbSong.songId}) to folder '{extractPath}'");

        string extractedTxtFilePath = extractedFiles
            .FirstOrDefault(path => path.EndsWith(".txt"));

        if (FileUtils.Exists(extractedTxtFilePath))
        {
            string txtContent = ReadPlainTextFileWithUnknownEncoding(extractedTxtFilePath);
            SearchCoverAndBackgroundImageInFolder(extractPath, out string coverImage, out string backgroundImage);
            return new UsdbSongDetails()
            {
                txtFile = extractedTxtFilePath,
                txtContent = txtContent,
                coverImage = coverImage,
                backgroundImage = backgroundImage,
            };
        }

        return new UsdbSongDetails();
    }

    private string ReadPlainTextFileWithUnknownEncoding(string filePath)
    {
        Encoding encoding = PlainTextReader.GuessUnknownFileEncodingUsingUniversalCharsetDetector(filePath);
        string fileContent = File.ReadAllText(filePath, encoding);
        return fileContent;
    }

    private string GetSongDetailsFolder(UsdbSong usdbSong)
    {
        string folderName = PathUtils.ReplaceInvalidFileNameChars($"{usdbSong.songId} - {usdbSong.artist} - {usdbSong.title}");
        return $"{modObjectContext.ModPersistentDataFolder}/songs/{folderName}";
    }

    private string GetNormalizedMatchGroupValue(Match match, int groupIndex)
    {
        string rawValue = match.Groups[groupIndex].Value;
        if (rawValue.IsNullOrEmpty())
        {
            return "";
        }

        return rawValue;
    }

    private List<UsdbSong> CreateUsdbSongsFromHtml(string html)
    {
        if (html.IsNullOrEmpty())
        {
            throw new Exception("Received empty HTML response from usdb.animux.de");
        }

        List<UsdbSong> result = new List<UsdbSong>();

        MatchCollection matchCollection = songListRowRegex.Matches(html);
        if (matchCollection.Count <= 0)
        {
            throw new Exception("Song row regex did not match anything in HTML response from usdb.animux.de");
        }

        Debug.Log($"Found {matchCollection.Count} song rows in HTML response from usdb.animux.de");
        foreach (Match match in matchCollection)
        {
            string songIdString = GetNormalizedMatchGroupValue(match, 1);
            string artist = GetNormalizedMatchGroupValue(match, 2);
            string title = GetNormalizedMatchGroupValue(match, 3);
            string edition = GetNormalizedMatchGroupValue(match, 4);
            string goldenNotesString = GetNormalizedMatchGroupValue(match, 5);
            string language = GetNormalizedMatchGroupValue(match, 6);
            string ratingString = GetNormalizedMatchGroupValue(match, 7);
            string viewsString = GetNormalizedMatchGroupValue(match, 8);

            if (!int.TryParse(songIdString, out int songIdInt))
            {
                throw new Exception($"Failed to convert song id from string to integer from regex match '{songIdString}'");
            }

            if (!int.TryParse(viewsString, out int viewsInt))
            {
                Debug.LogError($"Failed to convert view count from string to integer from regex match '{viewsString}'");
            }

            int ratingInt = StringUtils.CountOccurrencesInString(ratingString, "star.png");

            UsdbSong usdbSong = new UsdbSong
            {
                songId = songIdInt,
                artist = artist,
                title = title,
                edition = edition,
                language = language,
                goldenNotes = goldenNotesString.ToLower() == "yes",
                rating = ratingInt,
                views = viewsInt,
            };
            result.Add(usdbSong);
        }

        return result;
    }

    private async Task<FlurlCookie> GetNewSessionCookieAsync()
    {
        Debug.Log("Logging in to usdb.animux.de to get a new session cookie.");

        Dictionary<string, string> formData = new Dictionary<string, string>()
        {
            { "user", modSettings.username },
            { "pass", modSettings.password },
            { "login", "Login" },
        };
        IFlurlResponse response = await "https://usdb.animux.de"
            .WithHeader("User-Agent", "Some User Agent")
            .PostUrlEncodedAsync(formData);

        FlurlCookie sessionCookie = response.Cookies
            .FirstOrDefault(cookie => cookie.Name == PhpSessionIdCookieName);

        if (sessionCookie == null)
        {
            throw new Exception($"Failed to log in to usdb.animux.de with given username and password. Response status: {response.StatusCode}, response message: {response.ResponseMessage}");
        }
        else
        {
            Debug.Log($"Successfully logged in to usdb.animux.de.");
        }

        return sessionCookie;
    }

    private void SearchCoverAndBackgroundImageInFolder(string folder, out string coverImage, out string backgroundImage)
    {
        if (!DirectoryUtils.Exists(folder))
        {
            coverImage = "";
            backgroundImage = "";
            return;
        }

        List<string> folders = new List<string>()
        {
            folder
        };

        List<string> imageFileExtensionPatterns = new List<string>()
        {
            "*.png",
            "*.jpg",
        };

        List<string> imageFiles = FileScannerUtils.ScanForFiles(folders, imageFileExtensionPatterns);
        coverImage = imageFiles
            .FirstOrDefault(imageFile =>
            {
                string imageFileToLower = imageFile.ToLower();
                return imageFileToLower.Contains("cover")
                    || imageFileToLower.Contains("co")
                    || imageFileToLower.Contains("front")
                    || imageFileToLower.Contains("album");
            })
            .OrIfNull(imageFiles.FirstOrDefault())
            .OrIfNull("");

        backgroundImage = imageFiles
            .FirstOrDefault(imageFile =>
            {
                string imageFileToLower = imageFile.ToLower();
                return imageFileToLower.Contains("background")
                    || imageFileToLower.Contains("bg")
                    || imageFileToLower.Contains("back");
            })
            .OrIfNull(imageFiles.FirstOrDefault())
            .OrIfNull("");
    }

    public IObservable<SongRepositorySearchResultEntry> SearchSongs(SongRepositorySearchParameters searchParameters)
    {
        if (modSettings.loadFullSongIndex)
        {
            // Not searching dynamically on the website
            // because the full song index is loaded when the mod is loaded.
            return Observable.Empty<SongRepositorySearchResultEntry>();
        }

        if (modSettings.username.IsNullOrEmpty()
            || modSettings.password.IsNullOrEmpty())
        {
            Debug.Log("Not searching for songs on usdb.animux.de because no credentials are provided");
            return Observable.Empty<SongRepositorySearchResultEntry>();
        }

        return ObservableUtils.RunOnNewTaskAsObservableElements(async () =>
        {
            if (searchTermToSearchResult.TryGetValue(searchParameters.SearchText, out List<SongRepositorySearchResultEntry> cachedSearchResult))
            {
                return cachedSearchResult;
            }

            if (sessionCookie == null)
            {
                sessionCookie = await GetNewSessionCookieAsync();
            }

            // Search separately by artist and title
            List<UsdbSong> usdbSearchResultMatchingArtist = await SearchSongsAsync(sessionCookie, "", searchParameters.SearchText);
            List<UsdbSong> usdbSearchResultMatchingTitle = await SearchSongsAsync(sessionCookie, searchParameters.SearchText, "");

            List<UsdbSong> usdbSearchResult = usdbSearchResultMatchingArtist
                .Union(usdbSearchResultMatchingTitle)
                .ToList();

            List<SongMeta> songMetas = usdbSearchResult
                .Select(usdbSong => CreateSongMetaFromUsdbSong(usdbSong))
                .ToList();

            List<SongRepositorySearchResultEntry> searchResultEntries = songMetas
                .Select(songMeta => new SongRepositorySearchResultEntry(songMeta, new List<SongIssue>()))
                .ToList();

            // Cache search result
            searchTermToSearchResult[searchParameters.SearchText] = searchResultEntries;

            Debug.Log($"Returning {searchResultEntries.Count} search result entries for search text '{searchParameters.SearchText}'");

            return searchResultEntries;
        }, Disposable.Empty);
    }
}

public class UsdbAnimuxDeSongRepositoryModSettings : IModSettings, IOnBeforeSaveModSettings, IOnAfterLoadModSettings
{
    public bool loadFullSongIndex;
    public bool loadFullSongIndexDetails;
    public int maxSongIndexCacheAgeInDays = 7;

    // Do not write the credentials to disk by default,
    // i.e., ignore them in serialization to JSON via fsIgnore annotation.
    [fsIgnore]
    public string username = "";
    [fsIgnore]
    public string password = "";

    // Fields to save credentials
    public bool saveUserNameAndPassword;
    public string savedUsername = "";
    public string savedPassword = "";

    public void OnBeforeSaveModSettings()
    {
        Debug.Log("BeforeSaveModSettings");
        if (saveUserNameAndPassword)
        {
            savedUsername = username;
            savedPassword = password;
        }
        else
        {
            savedUsername = "";
            savedPassword = "";
        }
    }

    public void OnAfterLoadModSettings()
    {
        Debug.Log("AfterLoadingModSettings");
        if (saveUserNameAndPassword)
        {
            username = savedUsername;
            password = savedPassword;
        }
    }

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => username, newValue => username = newValue) { Label="User Name" },
            new StringModSettingControl(() => password, newValue => password = newValue) { Label="Password", IsPassword = true },
            new BoolModSettingControl(() => saveUserNameAndPassword, newValue => saveUserNameAndPassword = newValue) { Label="Save credentials (insecure, uses plain text)" },
            new BoolModSettingControl(() => loadFullSongIndex, newValue => loadFullSongIndex = newValue) { Label="Load full song index into cache" },
            new BoolModSettingControl(() => loadFullSongIndexDetails, newValue => loadFullSongIndexDetails = newValue) { Label="Load details of all songs in song index" },
            new IntModSettingControl(() => maxSongIndexCacheAgeInDays, newValue => maxSongIndexCacheAgeInDays = newValue) { Label="Max age of song index cache (days)" },
        };
    }
}

public class UsdbSong
{
    public int songId;
    public string artist;
    public string title;
    public string language;
    public string edition;
    public bool goldenNotes;
    public int rating;
    public int views;
}

public class UsdbSongDetails
{
    public string txtContent;
    public string txtFile;
    public string coverImage;
    public string backgroundImage;
}

public class UsdbSongIndex
{
    public Dictionary<int, UsdbSong> usdbSongIdToUsdbSong = new Dictionary<int, UsdbSong>();
    public DateTime synchronizationDateTime = DateTime.Now;
    public int Count => usdbSongIdToUsdbSong.Count;

    public void AddSongs(List<UsdbSong> usdbSongs)
    {
        usdbSongs.ForEach(usdbSong => AddSong(usdbSong));
    }

    private void AddSong(UsdbSong usdbSong)
    {
        if (usdbSong == null)
        {
            return;
        }

        usdbSongIdToUsdbSong[usdbSong.songId] = usdbSong;
    }
}

public class PersistedUsdbSongIndex
{
    public List<UsdbSong> songs = new List<UsdbSong>();
    public DateTime synchronizationDateTime = DateTime.Now;

    public static PersistedUsdbSongIndex FromSongIndex(UsdbSongIndex songIndex)
    {
        PersistedUsdbSongIndex result = new PersistedUsdbSongIndex();
        result.songs = songIndex.usdbSongIdToUsdbSong.Values
        .OrderBy(usdbSong => usdbSong.songId)
        .ToList();
        result.synchronizationDateTime = songIndex.synchronizationDateTime;
        return result;
    }

    public static UsdbSongIndex FromPersistedSongIndex(PersistedUsdbSongIndex persistedUsdbSongIndex)
    {
        UsdbSongIndex songIndex = new UsdbSongIndex();
        songIndex.AddSongs(persistedUsdbSongIndex.songs);
        songIndex.synchronizationDateTime = persistedUsdbSongIndex.synchronizationDateTime;
        return songIndex;
    }
}

public class UsdbUltraStarSongMeta : UltraStarSongMeta
{
    private enum ELoadDetailsPhase
    {
        Pending,
        Started,
        FinishedSuccessfully,
        Failed,
    }

    private Action onLoadDetails;
    public virtual Action OnLoadDetails
    {
        get
        {
            return onLoadDetails;
        }
        set
        {
            onLoadDetails = value;
            OnLoadVoices = value;
        }
    }

    public bool HasFailedToLoadDetails => loadDetailsPhase == ELoadDetailsPhase.Failed;
    private bool ShouldLoadDetails => loadDetailsPhase == ELoadDetailsPhase.Pending;
    private ELoadDetailsPhase loadDetailsPhase;

    public UsdbUltraStarSongMeta(string artist, string title)
        : base(artist, title, 200, "dummy-audio.ogg", new Dictionary<EVoiceId, string>())
    {
        RemoteSource = "usdb.animux.de";
    }

    public override string Cover
    {
        get
        {
            if (ShouldLoadDetails)
            {
                LoadDetails();
            }
            return base.Cover;
        }

        set
        {
            base.Cover = value;
        }
    }

    public override string Background
    {
        get
        {
            if (ShouldLoadDetails)
            {
                LoadDetails();
            }
            return base.Background;
        }

        set
        {
            base.Background = value;
        }
    }

    protected virtual void LoadDetails()
    {
        if (loadDetailsPhase != ELoadDetailsPhase.Pending)
        {
            return;
        }

        try
        {
            loadDetailsPhase = ELoadDetailsPhase.Started;
            if (OnLoadDetails == null)
            {
                throw new Exception($"Failed to load details of song '{SongMetaUtils.GetArtistDashTitle(this)}' because no lazy load action is set.");
            }
            else
            {
                OnLoadDetails();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to lazy load voices of '{SongMetaUtils.GetArtistDashTitle(this)}': {ex.Message}");
            loadDetailsPhase = ELoadDetailsPhase.Failed;
            return;
        }

        loadDetailsPhase = ELoadDetailsPhase.FinishedSuccessfully;
    }
}