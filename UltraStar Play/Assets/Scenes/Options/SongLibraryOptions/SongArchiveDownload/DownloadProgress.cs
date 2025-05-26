public class DownloadProgress
{
    public ulong DownloadedByteCount { get; private set; }
    public ulong FinalDownloadSizeInBytes { get; private set; }
    public double DownloadProgressInPercent { get; private set; }

    public DownloadProgress(ulong downloadedByteCount, ulong finalDownloadSizeInBytes)
    {
        DownloadedByteCount = downloadedByteCount;
        FinalDownloadSizeInBytes = finalDownloadSizeInBytes;
        DownloadProgressInPercent = finalDownloadSizeInBytes > 0
            ? 100.0 * ((double)downloadedByteCount / finalDownloadSizeInBytes)
            : 0.0;
    }
}
