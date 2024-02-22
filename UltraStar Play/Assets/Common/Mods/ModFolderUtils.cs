using System.Collections.Generic;
using System.Linq;

public static class ModFolderUtils
{
    public const string ModsRootFolderName = "Mods";

    public static List<string> AdditionalModRootFolders { get; set; } = new();

    public static List<string> GetModRootFolders()
    {
        return new List<string>()
            {
                GetDefaultModsRootFolderAbsolutePath(),
                GetUserDefinedModsRootFolderAbsolutePath(),
            }
            .Union(AdditionalModRootFolders)
            .ToList();
    }

    public static string GetDefaultModsRootFolderAbsolutePath()
    {
        return ApplicationUtils.GetStreamingAssetsPath(ModsRootFolderName);
    }

    public static string GetUserDefinedModsRootFolderAbsolutePath()
    {
        return ApplicationUtils.GetPersistentDataPath(ModsRootFolderName);
    }

}

