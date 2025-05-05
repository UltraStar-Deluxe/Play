using System.Collections.Generic;

public class DirectoryScannerConfig
{
    /**
     * Folder name patterns, e.g. "*Config"
     */
    public IReadOnlyCollection<string> SearchPatterns { get; set; }
    public bool ExcludeHiddenFolders { get; set; } = true;
    public bool Recursive { get; set; }

    public DirectoryScannerConfig()
    {
        SearchPatterns = new List<string> { "*" };
    }

    public DirectoryScannerConfig(IReadOnlyCollection<string> searchPatterns)
    {
        SearchPatterns = searchPatterns;
    }

    public DirectoryScannerConfig(params string[] fileExtensionPatterns)
    {
        SearchPatterns = fileExtensionPatterns;
    }
}
