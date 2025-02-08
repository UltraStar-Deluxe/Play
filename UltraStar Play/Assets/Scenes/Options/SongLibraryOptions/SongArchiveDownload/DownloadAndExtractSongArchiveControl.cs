using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class DownloadAndExtractSongArchiveControl
{
    private readonly string url;

    private string DownloadTargetFolder { get; set; }
    public string ExtractedArchiveTargetFolder { get; private set; }

    private bool wasStarted;
    private readonly FileDownloadControl fileDownloadControl;
    private readonly ExtractArchiveControl extractArchiveControl;

    public DownloadProgress DownloadProgress => new(fileDownloadControl.DownloadedBytes, finalDownloadSizeInBytes);
    public ExtractArchiveProgress ExtractionProgress => extractArchiveControl.Progress;

    private ulong finalDownloadSizeInBytes;

    public DownloadAndExtractSongArchiveControl(string url)
    {
        if (url.IsNullOrEmpty())
        {
            throw new ArgumentException("URL must not be empty");
        }

        this.url = url;
        DownloadTargetFolder = GetDownloadTargetPath();
        ExtractedArchiveTargetFolder = GetArchiveTargetFolder();
        fileDownloadControl = new FileDownloadControl(url, DownloadTargetFolder);
        extractArchiveControl = new ExtractArchiveControl(DownloadTargetFolder, ExtractedArchiveTargetFolder);
    }

    public async Awaitable DownloadAndExtractAsync()
    {
        if (wasStarted)
        {
            throw new Exception("Already started download");
        }
        wasStarted = true;

        await DownloadAsync();
        await ExtractAsync();
    }

    public void Cancel()
    {
        fileDownloadControl?.Cancel();
    }

    private async Awaitable DownloadAsync()
    {
        finalDownloadSizeInBytes = await fileDownloadControl.FetchFileSizeAsync();
        await fileDownloadControl.DownloadAsync();
    }

    private async Awaitable ExtractAsync()
    {
        await extractArchiveControl.ExtractArchiveAsync();
    }

    private string GetArchiveTargetFolder()
    {
        Uri uri = new Uri(url);
        string extractedArchiveName = $"{uri.Host}{uri.PathAndQuery}";
        if (extractedArchiveName.Contains("?"))
        {
            extractedArchiveName = extractedArchiveName.Substring(0, extractedArchiveName.IndexOf("?", StringComparison.InvariantCulture));
        }
        if (extractedArchiveName.StartsWith("/"))
        {
            extractedArchiveName = extractedArchiveName.Substring(1);
        }
        extractedArchiveName = Regex.Replace(extractedArchiveName, @"\W", "_");

        return ApplicationUtils.GetPersistentDataPath($"Songs/{extractedArchiveName}");
    }

    private string GetDownloadTargetPath()
    {
        Uri uri = new(url);
        string filename = Path.GetFileName(uri.LocalPath);
        string targetPath = $"{ApplicationManager.PersistentTempPath()}/{filename}";
        return targetPath;
    }
}
