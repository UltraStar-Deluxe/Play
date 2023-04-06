using System;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class DownloadAndExtractSongArchiveControl
{
    private readonly string url;
    private readonly Transform parentTransform;
    
    private bool wasDownloadStarted;
    private FileDownloadControl fileDownloadControl;
    private ExtractArchiveControl extractArchiveControl;
    
    public ReactiveProperty<bool> IsDone { get; private set; } = new();
    public ReactiveProperty<bool> HasError { get; private set; } = new();
    public ReactiveProperty<bool> IsDoneWithoutError { get; private set; } = new();
    public ReactiveProperty<bool> IsDoneOrHasError { get; private set; } = new();
    
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

        IsDone.Subscribe(newValue =>
        {
            IsDoneOrHasError.Value = IsDone.Value || HasError.Value;
            IsDoneWithoutError.Value = IsDone.Value && !HasError.Value;
        });
        HasError.Subscribe(newValue =>
        {
            IsDoneOrHasError.Value = IsDone.Value || HasError.Value;
            IsDoneWithoutError.Value = IsDone.Value && !HasError.Value;
        });
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
        fileDownloadControl.HasError.Subscribe(newValue =>
        {
            if (newValue)
            {
                HasError.Value = true;
                IsDone.Value = true;
            }
        });
        fileDownloadControl.ProgressEventStream.Subscribe(evt => downloadProgressEventStream.OnNext(evt));
        fileDownloadControl.SendWebRequest();
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

        string targetFolder = ApplicationUtils.GetPersistentDataPath("Songs");
        extractArchiveControl = ExtractArchiveControl.Create(archivePath, targetFolder, parentTransform);
        extractArchiveControl.BeforeDestroyEventStream.Subscribe(_ => extractArchiveControl = null);
        extractArchiveControl.IsDone.Subscribe(newValue =>
        {
            if (newValue)
            {
                IsDone.Value = true;
            }
        });
        extractArchiveControl.HasError.Subscribe(newValue =>
        {
            if (newValue)
            {
                HasError.Value = true;
            }
        });
        extractArchiveControl.ProgressEventStream.Subscribe(evt => extractProgressEventStream.OnNext(evt));
        extractArchiveControl.StartExtractArchive();
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
