using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using System.IO;
using UnityEngine.Networking;
using ICSharpCode.SharpZipLib.Tar;
using static ThreadPool;
using System.Text;
using PrimeInputActions;
using ProTrans;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContentDownloadSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    private static readonly List<string> defaultArchiveUrls = new List<string>
    {
        "https://42.usplay.net/ultrastar-songs-cc.tar"
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
    }

    private void StartDownload()
    {
        StartCoroutine(DownloadFileAsync(DownloadUrl, ApplicationManager.PersistentTempPath()));
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

    private IEnumerator DownloadFileAsync(string url, string targetFolder)
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

        Uri uri = new Uri(url);
        string filename = Path.GetFileName(uri.LocalPath);
        string targetPath = Path.Combine(targetFolder, filename);

        downloadRequest = UnityWebRequest.Get(url);
        DownloadHandlerFile downloadHandler = new DownloadHandlerFile(targetPath);
        downloadHandler.removeFileOnAbort = true;
        downloadRequest.downloadHandler = downloadHandler;

        StartCoroutine(TrackProgressAsync());

        yield return downloadRequest?.SendWebRequest();

        if (downloadRequest != null && (downloadRequest.isNetworkError || downloadRequest.isHttpError))
        {
            Debug.LogError($"Error downloading {url}: {downloadRequest.error}");
            AddToUiLog("Error downloading the requested file");
        }
        else if (downloadRequest != null)
        {
            statusLabel.text = "100%";
            AddToUiLog($"Download saved to {targetPath}. {downloadRequest.error}");
            UnpackTar(targetPath);
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
        logText.value = message + "\n" + logText.value;
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

        Debug.Log($"Fetching size of {url}");
        using (UnityWebRequest request = UnityWebRequest.Head(url))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {

                Debug.LogError($"Error fetching size: {request.error}");
                AddToUiLog($"Error fetching size: {request.error}");
                ResetFileSizeText();
            }
            else
            {
                long size = Convert.ToInt64(request.GetResponseHeader("Content-Length"));
                fileSize.text = (size / 1024 / 1024) + " MB";
            }
        }
    }

    private void UnpackTar(string tarPath)
    {
        if (downloadRequest == null)
        {
            return;
        }

        if (!File.Exists(tarPath))
        {
            AddToDebugAndUiLog("Can not unpack file because it does not exist on the storage! Did the download fail?", true);
            return;
        }

        AddToDebugAndUiLog("Preparing to unpack the downloaded song package.");
        string songsPath = ApplicationManager.PersistentSongsPath();
        PoolHandle handle = ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            using (Stream tarStream = File.OpenRead(tarPath))
            {
                using (TarArchive archive = TarArchive.CreateInputTarArchive(tarStream, Encoding.UTF8))
                {
                    try
                    {
                        archive.ExtractContents(songsPath);
                        poolHandle.done = true;
                    }
                    catch (Exception ex)
                    {
                        AddToDebugAndUiLog($"Unpacking failed: {ex.Message}");
                        SetErrorStatus();
                    }
                }
            }
        });
        StartCoroutine(TrackUnpackAsync(handle, tarPath, songsPath));
    }

    private IEnumerator TrackUnpackAsync(PoolHandle handle, string tarPath, string songsPath)
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
            AddToDebugAndUiLog($"Unpacking the song package failed. Handle was null for {tarPath}.", true);
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
            AddToDebugAndUiLog($"Unpacking the song package failed with an unknown error. Please check the log. File: {tarPath}", true);
            statusLabel.text = "Failed";
        }
        if (File.Exists(tarPath))
        {
            File.Delete(tarPath);
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
        fileSize.text = "??? MB";
    }
}
