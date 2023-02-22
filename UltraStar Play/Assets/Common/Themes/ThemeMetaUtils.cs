using System.IO;

public static class ThemeMetaUtils
{
    public static string GetDisplayName(ThemeMeta themeMeta)
    {
        if (themeMeta == null)
        {
            return "";
        }

        return StringUtils.ToTitleCase(themeMeta.FileNameWithoutExtension);
    }

    public static string GetAbsoluteFilePath(ThemeMeta themeMeta, string path)
    {
        if (WebRequestUtils.IsHttpOrHttpsUri(path)
            || WebRequestUtils.IsNetworkPath(path)
            || PathUtils.IsAbsolutePath(path))
        {
            return path;
        }

        return $"{Path.GetDirectoryName(themeMeta.AbsoluteFilePath)}/{path}";
    }

    public static bool HasStaticBackground(ThemeMeta themeMeta, Settings settings)
    {
        return !settings.DeveloperSettings.disableDynamicThemes
               && themeMeta.ThemeJson.staticBackground != null
               && !themeMeta.ThemeJson.staticBackground.imagePath.IsNullOrEmpty();
    }

    public static bool HasDynamicBackground(ThemeMeta themeMeta, Settings settings)
    {
        return !settings.DeveloperSettings.disableDynamicThemes
                && !HasStaticBackground(themeMeta, settings);
    }
}
