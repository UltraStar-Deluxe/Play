using System.IO;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

public class ExtractArchiveControl
{
    private readonly string archivePath;
    private readonly string targetFolder;

    private long totalZipEntryCount;
    private long extractedZipEntryCount;

    private long totalTarFileSizeInBytes;
    private long extractedTarFileEntrySizeInBytes;

    public ExtractArchiveProgress Progress
    {
        get
        {
            if (totalZipEntryCount > 0)
            {
                return new ExtractArchiveProgress(extractedZipEntryCount, totalZipEntryCount);
            }
            else if (totalTarFileSizeInBytes > 0)
            {
                return new ExtractArchiveProgress(extractedTarFileEntrySizeInBytes, totalTarFileSizeInBytes);
            }
            return new ExtractArchiveProgress(0, 0);
        }
    }

    public ExtractArchiveControl(string archivePath, string targetFolder)
    {
        this.archivePath = archivePath;
        this.targetFolder = targetFolder;
    }

    public async Awaitable ExtractArchiveAsync()
    {
        if (!FileUtils.Exists(archivePath))
        {
            throw new FileNotFoundException(archivePath);
        }

        if (!FileUtils.Exists(archivePath))
        {
            throw new FileNotFoundException(archivePath);
        }

        // Extract archive on background thread
        await Awaitable.BackgroundThreadAsync();

        if (archivePath.ToLowerInvariant().EndsWith("tar"))
        {
            await ExtractTarArchiveAsync();
        }
        else if (archivePath.ToLowerInvariant().EndsWith("zip"))
        {
            await ExtractZipArchiveAsync();
        }

        await Awaitable.MainThreadAsync();
    }

    private async Awaitable ExtractZipArchiveAsync()
    {
        using Stream archiveStream = File.OpenRead(archivePath);
        using ZipFile zipFile = new(archiveStream);
        extractedZipEntryCount = 0;
        totalZipEntryCount = zipFile.Count;

        foreach (ZipEntry entry in zipFile)
        {
            ExtractZipEntry(zipFile, entry);
        }
    }

    private void ExtractZipEntry(ZipFile zipFile, ZipEntry zipEntry)
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
    }

    private async Awaitable ExtractTarArchiveAsync()
    {
        await using Stream archiveStream = File.OpenRead(archivePath);
        using TarArchive archive = TarArchive.CreateInputTarArchive(archiveStream);
        archive.ProgressMessageEvent += OnExtractTarArchiveProgress;

        totalTarFileSizeInBytes = new FileInfo(archivePath).Length;
        archive.ExtractContents(targetFolder);
    }

    private void OnExtractTarArchiveProgress(TarArchive archive, TarEntry entry, string message)
    {
        extractedTarFileEntrySizeInBytes += entry.Size;
    }
}
