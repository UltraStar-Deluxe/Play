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

    public static bool HasStaticBackground(ThemeMeta themeMeta, Settings settings, EScene scene)
    {
        if (scene is EScene.SingScene)
        {
            // No theme background in sing scene
            return false;
        }
        
        StaticBackgroundJson staticBackgroundJson = GetStaticBackgroundJsonForScene(themeMeta, scene);
        return !settings.DisableDynamicThemes
               && (staticBackgroundJson != null
                   && !staticBackgroundJson.imagePath.IsNullOrEmpty());
    }

    public static StaticBackgroundJson GetStaticBackgroundJsonForScene(ThemeMeta themeMeta, EScene scene)
    {
        if (themeMeta.ThemeJson.sceneSpecificBackgrounds != null
            && themeMeta.ThemeJson.sceneSpecificBackgrounds.TryGetValue(scene.ToString(), out StaticAndDynamicBackgroundJson staticAndDynamicBackgroundJson)
            && staticAndDynamicBackgroundJson.staticBackground != null)
        {
            return staticAndDynamicBackgroundJson.staticBackground;
        }
        
        return themeMeta.ThemeJson.staticBackground;
    }

    public static bool HasDynamicBackground(ThemeMeta themeMeta, Settings settings, EScene scene)
    {
        if (scene is EScene.SingScene)
        {
            // No theme background in sing scene
            return false;
        }

        DynamicBackgroundJson dynamicBackgroundJson = GetDynamicBackgroundJsonForScene(themeMeta, scene);
        return !settings.DisableDynamicThemes
                && dynamicBackgroundJson != null;
    }

    public static DynamicBackgroundJson GetDynamicBackgroundJsonForScene(ThemeMeta themeMeta, EScene scene)
    {
        if (themeMeta.ThemeJson.sceneSpecificBackgrounds != null
            && themeMeta.ThemeJson.sceneSpecificBackgrounds.TryGetValue(scene.ToString(), out StaticAndDynamicBackgroundJson staticAndDynamicBackgroundJson)
            && staticAndDynamicBackgroundJson.dynamicBackground != null)
        {
            return staticAndDynamicBackgroundJson.dynamicBackground;
        }
        
        return themeMeta.ThemeJson.dynamicBackground;
    }
}
