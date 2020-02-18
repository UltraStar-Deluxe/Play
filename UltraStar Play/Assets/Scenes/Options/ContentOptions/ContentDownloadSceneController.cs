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
using UnityEngine.UIElements;
using ICSharpCode.SharpZipLib.Tar;
using static ThreadPool;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContentDownloadSceneController : MonoBehaviour, INeedInjection
{
    public Text statusLabel;
    public Text logText;
    public Text downloadPath;
    public Text fileSize;
    public SettingsManager settingsManager;

    public void StartDownload()
    {
        StartCoroutine(DownloadFile(downloadPath.text, PersistentTempPath()));
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

    private IEnumerator DownloadFile(string url, string targetFolder)
    {
        Uri uri = new Uri(url);
        string filename = Path.GetFileName(uri.LocalPath);
        string targetPath = Path.Combine(targetFolder, filename);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            AddToLog($"Starting file download for {uri}.");
            DownloadHandlerFile downloadHandler = new DownloadHandlerFile(targetPath);
            downloadHandler.removeFileOnAbort = true;
            request.downloadHandler = downloadHandler;

            StartCoroutine(TrackProgress(request, targetPath));

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                AddToLog("Error downloading the requested file!");
            }
            else
            {
                statusLabel.text = "100%";
                AddToLog($"Download saved to {targetPath}. {request.error}");
            }
        }

        yield return null;
    }

    private IEnumerator TrackProgress(UnityWebRequest req, string tarPath)
    {
        while (true)
        {
            try
            {
                if (req.downloadHandler.isDone)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                AddToLog(ex.Message);
                break;
            }
            statusLabel.text = Math.Round(req.downloadProgress * 100) + "%";
            yield return new WaitForSeconds(0.1f);
        }
        statusLabel.text = "100%";
        UnpackTar(tarPath);
    }

    private void AddToLog(string message)
    {
        logText.text = message + "\n" + logText.text;
    }

    public void UpdateFileSize()
    {
        StartCoroutine(FileSizeUpdate(downloadPath.text));
    }

    private IEnumerator FileSizeUpdate(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Head(url))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                AddToLog("Error publishing size of the requested file!");
                fileSize.text = "Unknown file size.";
            }
            else
            {
                long size = Convert.ToInt64(request.GetResponseHeader("Content-Length"));
                fileSize.text = (size / 1024 / 1024) + " MB";
            }
        }

        yield return null;
    }

    private void UnpackTar(string tarPath)
    {
        if (!File.Exists(tarPath))
        {
            AddToLog("Can not unpack file because it does not exist on the storage! Did the download fail?");
            return;
        }
        AddToLog("Preparing to unpack the downloaded song package.");
        string songsPath = PersistentSongsPath();
        PoolHandle handle = ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            using (Stream tarStream = File.OpenRead(tarPath))
            {
                using (TarArchive archive = TarArchive.CreateInputTarArchive(tarStream))
                {
                    archive.ExtractContents(songsPath);
                    poolHandle.done = true;
                }
            }
        });
        StartCoroutine(TrackUnpack(handle, tarPath, songsPath));
    }

    private IEnumerator TrackUnpack(PoolHandle handle, string tarPath, string songsPath)
    {
        string progress1 = "unpacking file.  ";
        string progress2 = "unpacking file.. ";
        string progress3 = "unpacking file...";

        while (handle != null && !handle.done)
        {
            statusLabel.text = progress1;
            yield return new WaitForSeconds(0.2f);
            if (handle == null || handle.done)
            {
                break;
            }
            statusLabel.text = progress2;
            yield return new WaitForSeconds(0.2f);
            if (handle == null || handle.done)
            {
                break;
            }
            statusLabel.text = progress3;
            yield return new WaitForSeconds(0.2f);
        }

        if (handle == null)
        {
            AddToLog($"Unpacking the song package failed! Handle was null for {tarPath}!");
            statusLabel.text = "Failed. Handle was null";

        }
        else if (handle.done)
        {
            AddToLog($"Unpacking the song package finished for {tarPath}.");
            statusLabel.text = "Finished.";
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
            AddToLog($"Unpacking the song package failed! Unknown error, check log. File: {tarPath}!");
            statusLabel.text = "Failed.";
        }
        if (File.Exists(tarPath))
        {
            File.Delete(tarPath);
        }
    }
}
