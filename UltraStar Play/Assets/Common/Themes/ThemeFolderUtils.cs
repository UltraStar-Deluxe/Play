using System.Collections.Generic;
using System.Linq;

public static class ThemeFolderUtils
{
    public const string ThemeFolderName = "Themes";

    public static List<string> AdditionalThemeFolders { get; set; } = new();

    public static List<string> GetThemeFolders()
    {
        return new List<string>
        {
            GetDefaultThemesFolderAbsolutePath(),
            GetUserDefinedThemesFolderAbsolutePath(),
        }
        .Union(AdditionalThemeFolders)
        .Distinct()
        .ToList();
    }

    public static string GetUserDefinedThemesFolderAbsolutePath()
    {
        return ApplicationUtils.GetPersistentDataPath(ThemeFolderName);
    }

    public static string GetDefaultThemesFolderAbsolutePath()
    {
        return ApplicationUtils.GetStreamingAssetsPath(ThemeFolderName);
    }
}
