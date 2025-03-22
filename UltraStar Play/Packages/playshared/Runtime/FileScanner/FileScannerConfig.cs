using System.Collections.Generic;

public class FileScannerConfig
{
    /**
     * File name patterns, e.g. "*.txt"
     */
    public IReadOnlyCollection<string> SearchPatterns { get; set; }
    public bool ExcludeHiddenFolders { get; set; } = true;
    public bool ExcludeHiddenFiles { get; set; } = true;
    public bool Recursive { get; set; }

    public FileScannerConfig()
    {
        SearchPatterns = new List<string> { "*" };
    }

    public FileScannerConfig(IReadOnlyCollection<string> searchPatterns)
    {
        SearchPatterns = searchPatterns;
    }

    public FileScannerConfig(params string[] searchPatterns)
    {
        SearchPatterns = searchPatterns;
    }
}
