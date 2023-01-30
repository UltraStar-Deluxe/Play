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

    public static string GetAbsoluteFilePath(ThemeMeta themeMeta, string themeRelativePath)
    {
        return $"{Path.GetDirectoryName(themeMeta.AbsoluteFilePath)}/{themeRelativePath}";
    }

    public static bool HasStaticBackground(ThemeMeta themeMeta)
    {
        return themeMeta.ThemeJson.staticBackground != null
               && !themeMeta.ThemeJson.staticBackground.imagePath.IsNullOrEmpty();
    }

    public static bool HasDynamicBackground(ThemeMeta themeMeta)
    {
        return !HasStaticBackground(themeMeta);
    }
}
