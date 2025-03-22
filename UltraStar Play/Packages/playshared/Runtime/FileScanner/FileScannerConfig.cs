using System.Collections.Generic;

public class FileScannerConfig
{
    public IReadOnlyCollection<string> FileExtensionPatterns { get; set; }
    public bool ExcludeHiddenFolders { get; set; } = true;
    public bool ExcludeHiddenFiles { get; set; } = true;
    public bool Recursive { get; set; }

    public FileScannerConfig(IReadOnlyCollection<string> fileExtensionPatterns)
    {
        FileExtensionPatterns = fileExtensionPatterns;
    }

    public FileScannerConfig(params string[] fileExtensionPatterns)
    {
        FileExtensionPatterns = fileExtensionPatterns;
    }
}
