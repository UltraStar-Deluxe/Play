using System;
using System.IO;
using System.Text.RegularExpressions;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class DownloadAndExtractSongArchiveControl
{
    private readonly string url;
    private readonly Transform parentTransform;
    
    public string TargetFolder => extractArchiveControl != null
        ? extractArchiveControl.TargetFolder
        : "";
    
    private bool wasDownloadStarted;
    private FileDownloadControl fileDownloadControl;
    private ExtractArchiveControl extractArchiveControl;
    
    public ReactiveProperty<bool> IsDone { get; private set; } = new();
    public ReactiveProperty<string> ErrorMessage { get; private set; } = new();
    public IObservable<bool> HasError => ErrorMessage.Select(errorMessage => !errorMessage.IsNullOrEmpty());
    public IObservable<bool> IsDoneOrHasError => IsDone.CombineLatest(HasError, (isDone, hasError) => isDone || hasError);
    public IObservable<bool> IsDoneWithoutError => IsDone.CombineLatest(HasError, (isDone, hasError) => isDone && !hasError);
    
    private readonly Subject<FileDownloadControl.DownloadProgressEvent> downloadProgressEventStream = new();
    public IObservable<FileDownloadControl.DownloadProgressEvent> DownloadProgressEventStream => downloadProgressEventStream;
    
    private readonly Subject<ExtractArchiveControl.ExtractArchiveProgressEvent> extractProgressEventStream = new();
    public IObservable<ExtractArchiveControl.ExtractArchiveProgressEvent> ExtractProgressEventStream => extractProgressEventStream;

    public DownloadAndExtractSongArchiveControl(string url, Transform parentTransform)
    {
        if (url.IsNullOrEmpty())
        {
            throw new ArgumentException("URL must not be empty");
        }
        
        this.url = url;
        this.parentTransform = parentTransform;
    }
    
    public void Start()
    {
        if (wasDownloadStarted)
        {
            throw new Exception("Already started download");
        }
        wasDownloadStarted = true;
        
        if (extractArchiveControl != null)
        {
            throw new Exception("Extracting archive still in progress");
        }
        
        string targetPath = GetDownloadTargetPath(url);

        try
        {
            UnityWebRequest webRequest = FileDownloadControl.CreateDownloadRequest(url, targetPath);
            fileDownloadControl = FileDownloadControl.Create(webRequest, parentTransform);
            fileDownloadControl.BeforeDestroyEventStream.Subscribe(_ => fileDownloadControl = null);
            fileDownloadControl.IsDoneWithoutError.Subscribe(newValue =>
            {
                if (newValue)
                {
                    StartExtractArchive(targetPath);
                }
            });
            fileDownloadControl.ErrorMessage.Subscribe(newValue =>
            {
                if (!newValue.IsNullOrEmpty())
                {
                    ErrorMessage.Value = newValue;
                    IsDone.Value = true;
                }
            });
            fileDownloadControl.ProgressEventStream.Subscribe(evt => downloadProgressEventStream.OnNext(evt));
            fileDownloadControl.SendWebRequest();
        }
        catch (Exception ex)
        {
            ErrorMessage.Value = ex.Message;
            IsDone.Value = true;
        }
    }
    
    private void StartExtractArchive(string archivePath)
    {
        if (extractArchiveControl != null)
        {
            throw new Exception("Extracting archive still in progress");
        }
            
        if (!FileUtils.Exists(archivePath))
        {
            throw new FileNotFoundException(archivePath);
        }

        string targetFolder = GetArchiveTargetFolder();
        extractArchiveControl = ExtractArchiveControl.Create(archivePath, targetFolder, parentTransform);
        extractArchiveControl.BeforeDestroyEventStream.Subscribe(_ => extractArchiveControl = null);
        extractArchiveControl.IsDone.Subscribe(newValue =>
        {
            if (newValue)
            {
                IsDone.Value = true;
            }
        });
        extractArchiveControl.ErrorMessage.Subscribe(newValue =>
        {
            if (!newValue.IsNullOrEmpty())
            {
                ErrorMessage.Value = newValue;
                IsDone.Value = true;
            }
        });
        extractArchiveControl.ProgressEventStream.Subscribe(evt => extractProgressEventStream.OnNext(evt));
        extractArchiveControl.StartExtractArchive();
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

    public void Cancel()
    {
        if (fileDownloadControl != null)
        {
            fileDownloadControl.Cancel();
        }
        else if (extractArchiveControl != null)
        {
            extractArchiveControl.Cancel();
        }
    }
    
    private static string GetDownloadTargetPath(string url)
    {
        Uri uri = new(url);
        string filename = Path.GetFileName(uri.LocalPath);
        string targetPath = ApplicationManager.PersistentTempPath() + "/" + filename;
        return targetPath;
    }
}
