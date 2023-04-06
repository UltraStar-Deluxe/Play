using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using UniRx;
using UnityEngine;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

public class ExtractArchiveControl : MonoBehaviour
{
    public string ArchivePath { get; private set; }
    public string TargetFolder { get; private set; }

    public ReactiveProperty<bool> IsDone { get; private set; } = new();
    public ReactiveProperty<bool> HasError { get; private set; } = new();
    public ReactiveProperty<bool> IsDoneWithoutError { get; private set; } = new();
    public ReactiveProperty<bool> IsDoneOrHasError { get; private set; } = new();

    private long totalZipEntryCount;
    private long extractedZipEntryCount;
    
    private long totalTarFileSizeInBytes;
    private long extractedTarFileEntrySizeInBytes;
    
    private readonly Subject<ExtractArchiveProgressEvent> progressEventStream = new();
    public IObservable<ExtractArchiveProgressEvent> ProgressEventStream => progressEventStream;
    
    private readonly Subject<bool> beforeDestroyEventStream = new();
    public IObservable<bool> BeforeDestroyEventStream => beforeDestroyEventStream;
    
    private bool isExtractArchiveStarted;

    public static ExtractArchiveControl Create(string archivePath, string targetFolder, Transform parent)
    {
        string name = $"{nameof(ExtractArchiveControl)} {archivePath}";
        GameObject gameObject = new GameObject(name);
        if (parent != null)
        {
            gameObject.transform.parent = parent;
        }

        ExtractArchiveControl extractArchiveControl = gameObject.AddComponent<ExtractArchiveControl>();
        extractArchiveControl.ArchivePath = archivePath;
        extractArchiveControl.TargetFolder = targetFolder;
        extractArchiveControl.Init();
        return extractArchiveControl;
    }

    private void Init()
    {
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

    public void StartExtractArchive()
    {
        if (isExtractArchiveStarted)
        {
            throw new Exception("Already extracting archive");
        }

        if (!FileUtils.Exists(ArchivePath))
        {
            throw new FileNotFoundException(ArchivePath);
        }

        isExtractArchiveStarted = true;
        ThreadPool.QueueUserWorkItem(_ => ExtractArchive());
    }

    private void ExtractArchive()
    {
        try
        {
            if (!FileUtils.Exists(ArchivePath))
            {
                throw new FileNotFoundException(ArchivePath);
            }

            if (ArchivePath.ToLowerInvariant().EndsWith("tar"))
            {
                ExtractTarArchive();
            }
            else if (ArchivePath.ToLowerInvariant().EndsWith("zip"))
            {
                ExtractZipArchive();
            }
            progressEventStream.OnNext(new ExtractArchiveProgressEvent(totalZipEntryCount, totalZipEntryCount));
            IsDone.Value = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to extract archive '{ArchivePath}': {e.Message}");
            HasError.Value = true;
        }
    }
    
    private void ExtractZipArchive()
    {
        using Stream archiveStream = File.OpenRead(ArchivePath);
        using ZipFile zipFile = new(archiveStream);
        extractedZipEntryCount = 0;
        totalZipEntryCount = zipFile.Count;

        foreach (ZipEntry entry in zipFile)
        {
            if (this == null)
            {
                throw new Exception("Object destroyed");
            }
            
            ExtractZipEntry(zipFile, entry, TargetFolder);
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

        Debug.Log($"Extracting ZIP entry {entryPath}");
        string targetFilePath = targetFolder + "/" + entryPath;

        byte[] buffer = new byte[4096];
        using Stream zipEntryStream = zipFile.GetInputStream(zipEntry);
        using FileStream targetFileStream = File.Create(targetFilePath);
        StreamUtils.Copy(zipEntryStream, targetFileStream, buffer);
        
        extractedZipEntryCount++;
        progressEventStream.OnNext(new ExtractArchiveProgressEvent(extractedZipEntryCount, totalZipEntryCount));
    }

    private void ExtractTarArchive()
    {
        using Stream archiveStream = File.OpenRead(ArchivePath);
        using TarArchive archive = TarArchive.CreateInputTarArchive(archiveStream, Encoding.UTF8);
        archive.ProgressMessageEvent += OnExtractTarArchiveProgress;

        totalTarFileSizeInBytes = new FileInfo(ArchivePath).Length;
        archive.ExtractContents(TargetFolder);
    }

    private void OnExtractTarArchiveProgress(TarArchive archive, TarEntry entry, string message)
    {
        extractedTarFileEntrySizeInBytes += entry.Size;
        progressEventStream.OnNext(new ExtractArchiveProgressEvent(extractedTarFileEntrySizeInBytes, totalTarFileSizeInBytes));
    }

    private void Update()
    {
        if (!isExtractArchiveStarted)
        {
            return;
        }

        if (IsDoneOrHasError.Value)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        beforeDestroyEventStream.OnNext(true);
    }

    public class ExtractArchiveProgressEvent
    {
        public double ProgressInPercent { get; private set; }

        public ExtractArchiveProgressEvent(long extractedSize, long totalSize)
        {
            ProgressInPercent = totalSize > 0 
                ? 100.0 * (double)extractedSize / totalSize
                : 0.0;
        }
    }
}
