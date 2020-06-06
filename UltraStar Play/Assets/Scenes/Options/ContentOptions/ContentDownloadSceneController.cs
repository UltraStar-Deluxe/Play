using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.IO;
using UnityEngine.Networking;
using ICSharpCode.SharpZipLib.Tar;
using static ThreadPool;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContentDownloadSceneController : MonoBehaviour, INeedInjection
{
    public Text statusLabel;
    public Text logText;
    public InputField downloadPath;
    public Text fileSize;
    public Button startDownloadButton;
    public Button cancelDownloadButton;

    private string downloadUrl => downloadPath.text.Trim();

    private UnityWebRequest downloadRequest;

    void Start()
    {
        startDownloadButton.OnClickAsObservable().Subscribe(_ => StartDownload());
        cancelDownloadButton.OnClickAsObservable().Subscribe(_ => CancelDownload());
        downloadPath.OnEndEditAsObservable().Subscribe(_ => FetchFileSize());
        if (!downloadUrl.IsNullOrEmpty())
        {
            FetchFileSize();
        }
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
        StartCoroutine(DownloadFileAsync(downloadUrl, PersistentTempPath()));
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
        StartCoroutine(FileSizeUpdateAsync(downloadUrl));
    }

    private string PersistentTempPath()
    {
        string path = Path.Combine(Application.persistentDataPath, "Temp");
        //Create Directory if it does not exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private string PersistentSongsPath()
    {
        string path = Path.Combine(Application.persistentDataPath, "Songs");
        //Create Directory if it does not exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
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
        logText.text = message + "\n" + logText.text;
    }

    public void UpdateFileSize()
    {
        StartCoroutine(FileSizeUpdateAsync(downloadUrl));
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

                Debug.LogError($"Error fetching size: <color='red'>{request.error}</color>");
                AddToUiLog($"Error fetching size: <color='red'>{request.error}</color>");
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
        string songsPath = PersistentSongsPath();
        PoolHandle handle = ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            using (Stream tarStream = File.OpenRead(tarPath))
            {
                using (TarArchive archive = TarArchive.CreateInputTarArchive(tarStream))
                {
                    try
                    {
                        archive.ExtractContents(songsPath);
                        poolHandle.done = true;
                    }
                    catch (Exception ex)
                    {
                        AddToDebugAndUiLog($"Unpacking failed: <color='red'>{ex.Message}</color>");
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
            downloadPath.text = "";
            List<string> songDirs = SettingsManager.Instance.Settings.GameSettings.songDirs;
            if (!songDirs.Contains(songsPath))
            {
                songDirs.Add(songsPath);
                SettingsManager.Instance.Save();
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
        statusLabel.text = "Finished";
    }

    private void SetErrorStatus()
    {
        statusLabel.text = "Failed";
    }

    private void SetCanceledStatus()
    {
        statusLabel.text = "Canceled";
    }

    private void ResetFileSizeText()
    {
        fileSize.text = "Unknown file size";
    }
}
