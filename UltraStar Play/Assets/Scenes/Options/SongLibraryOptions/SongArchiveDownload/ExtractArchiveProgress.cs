public class ExtractArchiveProgress
{
    public double ProgressInPercent { get; private set; }

    public ExtractArchiveProgress(long extractedSize, long totalSize)
    {
        ProgressInPercent = totalSize > 0
            ? 100.0 * (double)extractedSize / totalSize
            : 0.0;
    }
}
