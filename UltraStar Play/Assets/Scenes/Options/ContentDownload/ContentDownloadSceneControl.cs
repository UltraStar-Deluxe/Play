using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using static ThreadPool;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContentDownloadSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    private static readonly List<string> defaultArchiveUrls = new()
    {
        "https://github.com/UltraStar-Deluxe/songs-stream/archive/refs/heads/main.zip",
        "https://42.usplay.net/ultrastar-songs-cc.tar",
    };

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.statusLabel)]
    private Label statusLabel;

    [Inject(UxmlName = R.UxmlNames.outputTextField)]
    private TextField logText;

    [Inject(UxmlName = R.UxmlNames.urlLabel)]
    private Label urlLabel;

    [Inject(UxmlName = R.UxmlNames.urlChooser)]
    private DropdownField urlChooser;

    [Inject(UxmlName = R.UxmlNames.urlTextField)]
    private TextField downloadPath;

    [Inject(UxmlName = R.UxmlNames.sizeLabel)]
    private Label fileSize;

    [Inject(UxmlName = R.UxmlNames.startButton)]
    private Button startDownloadButton;

    [Inject(UxmlName = R.UxmlNames.cancelButton)]
    private Button cancelDownloadButton;

    [Inject]
    private SettingsManager settingsManager;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private SongMetaManager songMetaManager;

    private string DownloadUrl => downloadPath.value.Trim();

    private UnityWebRequest downloadRequest;

    // The Ui log may only be filled from the main thread.
    // This string caches new log lines for other threads.
    private string newLogText;

    void Start()
    {
        statusLabel.text = "";
        urlChooser.choices = new List<string>(defaultArchiveUrls);
        urlChooser.value = urlChooser.choices[0];
        downloadPath.value = defaultArchiveUrls[0];

        urlChooser.RegisterValueChangedCallback(evt => downloadPath.value = evt.newValue);

        startDownloadButton.RegisterCallbackButtonTriggered(() => StartDownload());
        cancelDownloadButton.RegisterCallbackButtonTriggered(() => CancelDownload());
        downloadPath.RegisterValueChangedCallback(evt => FetchFileSize());
        if (!DownloadUrl.IsNullOrEmpty())
        {
            FetchFileSize();
        }

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.SongLibraryOptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.SongLibraryOptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        urlLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_archiveUrlLabel);
        startDownloadButton.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_startDownloadButton);
        cancelDownloadButton.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_cancelDownloadButton);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_title);
    }

    void Update()
    {
        if (downloadRequest != null
            && (downloadRequest.isDone || !downloadRequest.error.IsNullOrEmpty()))
        {
            Debug.Log("Disposing downloadRequest");
            downloadRequest.Dispose();
            downloadRequest = null;
            SetCanceledStatus();
        }

        if (!newLogText.IsNullOrEmpty())
        {
            logText.value = newLogText + logText.value;
            newLogText = "";
        }
    }

    private string GetDownloadTargetPath(string url)
    {
        Uri uri = new(url);
        string filename = Path.GetFileName(uri.LocalPath);
        string targetPath = ApplicationManager.PersistentTempPath() + "/" + filename;
        return targetPath;
    }

    private DownloadHandler CreateDownloadHandler(string targetPath)
    {
        DownloadHandlerFile downloadHandler = new(targetPath);
        downloadHandler.removeFileOnAbort = true;
        return downloadHandler;
    }

    private void StartDownload()
    {
        StartCoroutine(DownloadFileAsync(DownloadUrl));
    }

    private void CancelDownload()
    {
        if (downloadRequest != null && !downloadRequest.isDone)
        {
            Debug.Log("Aborting download");
            downloadRequest.Abort();
            AddToUiLog("Canceled download");
        }
    }

    private void FetchFileSize()
    {
        StartCoroutine(FileSizeUpdateAsync(DownloadUrl));
    }

    private IEnumerator DownloadFileAsync(string url)
    {
        if (downloadRequest != null
            && !downloadRequest.isDone)
        {
            AddToUiLog("Download in progress. Cancel the other download first.");
            yield break;
        }

        if (url.IsNullOrEmpty())
        {
            yield break;
        }

        Debug.Log($"Started download: {url}");
        AddToUiLog($"Started download");

        string targetPath = GetDownloadTargetPath(url);
        DownloadHandler downloadHandler = CreateDownloadHandler(targetPath);

        downloadRequest = UnityWebRequest.Get(url);
        downloadRequest.downloadHandler = downloadHandler;

        StartCoroutine(TrackProgressAsync());

        yield return downloadRequest?.SendWebRequest();

        if (downloadRequest is { result: UnityWebRequest.Result.ConnectionError
                                      or UnityWebRequest.Result.ProtocolError })
        {
            Debug.LogError($"Error downloading {url}: {downloadRequest.error}");
            AddToUiLog("Error downloading the requested file");
        }
        else if (downloadRequest != null)
        {
            statusLabel.text = "100%";
            AddToUiLog($"Download saved to {targetPath}. {downloadRequest.error}");
            UnpackArchive(targetPath);
        }
    }

    private IEnumerator TrackProgressAsync()
    {
        while (downloadRequest != null
            && downloadRequest.downloadHandler != null
            && !downloadRequest.downloadHandler.isDone)
        {
            float progress;
            try
            {
                progress = downloadRequest.downloadProgress;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                AddToUiLog(ex.Message);
                statusLabel.text = "?";
                yield break;
            }
            string progressText = Math.Round(progress * 100) + "%";
            statusLabel.text = progressText;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void AddToDebugAndUiLog(string message, bool isError = false)
    {
        if (isError)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
        AddToUiLog(message);
    }

    private void AddToUiLog(string message)
    {
        newLogText += message + "\n";
    }

    public void UpdateFileSize()
    {
        StartCoroutine(FileSizeUpdateAsync(DownloadUrl));
    }

    private IEnumerator FileSizeUpdateAsync(string url)
    {
        if (url.IsNullOrEmpty())
        {
            // Do not continue with the coroutine
            ResetFileSizeText();
            yield break;
        }

        using UnityWebRequest request = UnityWebRequest.Head(url);
        yield return request.SendWebRequest();

        if (request.result
            is UnityWebRequest.Result.ConnectionError
            or UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error fetching size: {request.error}");
            AddToUiLog($"Error fetching size: {request.error}");
            ResetFileSizeText();
        }
        else
        {
            string contentLength = request.GetResponseHeader("Content-Length");
            if (contentLength.IsNullOrEmpty())
            {
                ResetFileSizeText();
            }
            else
            {
                long size = Convert.ToInt64(contentLength);
                long kiloByte = size / 1024;
                if (kiloByte > 1024)
                {
                    fileSize.text = Math.Round((double)kiloByte / 1024) + " MB";
                }
                else
                {
                    fileSize.text = Math.Round((double)kiloByte) + " KB";
                }
            }
        }
    }

    private void UnpackArchive(string archivePath)
    {
        if (downloadRequest == null)
        {
            return;
        }

        if (!File.Exists(archivePath))
        {
            AddToDebugAndUiLog("Can not unpack file because it does not exist on the storage! Did the download fail?", true);
            return;
        }

        AddToDebugAndUiLog("Preparing to unpack the downloaded song package.");
        string songsPath = ApplicationManager.PersistentSongsPath();
        PoolHandle handle = ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            if (archivePath.ToLowerInvariant().EndsWith("tar"))
            {
                ExtractTarArchive(archivePath, songsPath, poolHandle);
            }
            else if (archivePath.ToLowerInvariant().EndsWith("zip"))
            {
                ExtractZipArchive(archivePath, songsPath, poolHandle);
            }
        });
        StartCoroutine(TrackUnpackAsync(handle, archivePath, songsPath));
    }

    private void ExtractZipArchive(string archivePath, string targetFolder, PoolHandle poolHandle)
    {
        using Stream archiveStream = File.OpenRead(archivePath);
        using ZipFile zipFile = new(archiveStream);

        try
        {
            foreach (ZipEntry entry in zipFile)
            {
                ExtractZipEntry(zipFile, entry, targetFolder);
            }

            poolHandle.done = true;
        }
        catch (Exception ex)
        {
            AddToDebugAndUiLog($"Unpacking failed: {ex.Message}");
            SetErrorStatus();
        }
    }

    private void ExtractZipEntry(ZipFile zipFile, ZipEntry zipEntry, string targetFolder)
    {
        string entryPath = zipEntry.Name;
        if (!zipEntry.IsFile)
        {
            string targetSubFolder = targetFolder + "/" + zipEntry.Name;
            Directory.CreateDirectory(targetSubFolder);
            return;
        }

        Debug.Log($"Extracting {entryPath}");
        string targetFilePath = targetFolder + "/" + entryPath;

        byte[] buffer = new byte[4096];
        using Stream zipEntryStream = zipFile.GetInputStream(zipEntry);
        using FileStream targetFileStream = File.Create(targetFilePath);
        StreamUtils.Copy(zipEntryStream, targetFileStream, buffer);
    }

    private void ExtractTarArchive(string archivePath, string targetFolder, PoolHandle poolHandle)
    {
        using Stream archiveStream = File.OpenRead(archivePath);
        using TarArchive archive = TarArchive.CreateInputTarArchive(archiveStream, Encoding.UTF8);

        try
        {
            archive.ExtractContents(targetFolder);
            poolHandle.done = true;
        }
        catch (Exception ex)
        {
            AddToDebugAndUiLog($"Unpacking failed: {ex.Message}");
            SetErrorStatus();
        }
    }

    private IEnumerator TrackUnpackAsync(PoolHandle handle, string archivePath, string songsPath)
    {
        if (downloadRequest == null)
        {
            yield break;
        }

        string progress1 = "Unpacking file.  ";
        string progress2 = "Unpacking file.. ";
        string progress3 = "Unpacking file...";

        while (handle != null && !handle.done)
        {
            statusLabel.text = progress1;
            yield return new WaitForSeconds(0.5f);
            if (handle == null || handle.done)
            {
                break;
            }
            statusLabel.text = progress2;
            yield return new WaitForSeconds(0.5f);
            if (handle == null || handle.done)
            {
                break;
            }
            statusLabel.text = progress3;
            yield return new WaitForSeconds(0.5f);
        }

        if (handle == null)
        {
            AddToDebugAndUiLog($"Unpacking the song package failed. Handle was null for {archivePath}.", true);
            statusLabel.text = "Failed. Handle was null";

        }
        else if (handle.done)
        {
            AddToDebugAndUiLog($"Finished unpacking the song package to {songsPath}");
            SetFinishedStatus();
            downloadPath.value = "";
            List<string> songDirs = settings.GameSettings.songDirs;
            if (!songDirs.Contains(songsPath))
            {
                songDirs.Add(songsPath);
                settingsManager.Save();
            }
            // Reload SongMetas if they had been loaded already.
            if (SongMetaManager.IsSongScanFinished)
            {
                Debug.Log("Rescan songs after successful download.");
                SongMetaManager.ResetSongMetas();
                songMetaManager.ScanFilesIfNotDoneYet();
            }
        }
        else
        {
            AddToDebugAndUiLog($"Unpacking the song package failed with an unknown error. Please check the log. File: {archivePath}", true);
            statusLabel.text = "Failed";
        }
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }
    }

    private void SetFinishedStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_finished);
    }

    private void SetErrorStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_failed);
    }

    private void SetCanceledStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_canceled);
    }

    private void ResetFileSizeText()
    {
        fileSize.text = "??? KB";
    }
}
