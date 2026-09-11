using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace DefaultNamespace;

public static class FileDownloader
{
    private static readonly HttpClient httpClient = new();

    public static async Task DownloadFilesAsync(string baseUrl, AbsolutePath targetDir, params string[] relativeFilePaths)
    {
        foreach (string relativePath in relativeFilePaths)
        {
            await DownloadFileAsync($"{baseUrl}/{relativePath}", targetDir / relativePath);
        }
    }

    public static async Task DownloadFileAsync(string url, AbsolutePath targetFile)
    {
        if (File.Exists(targetFile) && new FileInfo(targetFile).Length > 0)
        {
            Console.WriteLine($"File already exists, skipping download: '{targetFile}'");
            return;
        }

        Console.WriteLine($"Downloading '{url}' to '{targetFile}'");
        DirectoryUtils.CreateDirectory(targetFile.Parent);

        AbsolutePath tempFile = (AbsolutePath)$"{targetFile}.tmp";
        FileUtils.DeleteFile(tempFile);

        using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            long? totalBytes = response.Content.Headers.ContentLength;
            await using var sourceStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[81920];
            long totalBytesRead = 0;
            int bytesRead;
            Stopwatch stopwatch = Stopwatch.StartNew();
            long lastLogMs = 0;

            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                if (stopwatch.ElapsedMilliseconds - lastLogMs >= 3000)
                {
                    lastLogMs = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine(FormatProgress(targetFile.Name, totalBytesRead, totalBytes));
                }
            }
        }

        FileUtils.MoveFile(tempFile, targetFile, new FileUtils.FileMoveSettings { Overwrite = true });
        Console.WriteLine($"Done downloading '{targetFile}'");
    }

    private static string FormatProgress(string fileName, long bytesRead, long? totalBytes)
    {
        double currentMb = bytesRead / (1024.0 * 1024.0);
        if (totalBytes is > 0)
        {
            double totalMb = totalBytes.Value / (1024.0 * 1024.0);
            double percent = (double)bytesRead / totalBytes.Value * 100.0;
            return $"Downloading '{fileName}': {currentMb:F1} MB / {totalMb:F1} MB ({percent:F0}%)";
        }

        return $"Downloading '{fileName}': {currentMb:F1} MB";
    }
}
