using System.IO;
using UnityEngine;

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
        return !settings.DeveloperSettings.disableDynamicThemes
               && (staticBackgroundJson != null
                   && !staticBackgroundJson.imagePath.IsNullOrEmpty());
    }

    public static StaticBackgroundJson GetStaticBackgroundJsonForScene(ThemeMeta themeMeta, EScene scene)
    {
        if (themeMeta.ThemeJson.sceneSpecificStaticBackgrounds != null
            && themeMeta.ThemeJson.sceneSpecificStaticBackgrounds.TryGetValue(scene.ToString(), out StaticBackgroundJson staticBackgroundJson))
        {
            return staticBackgroundJson;
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
        
        return !settings.DeveloperSettings.disableDynamicThemes
                && themeMeta.ThemeJson.dynamicBackground != null;
    }

    public static Color32 GetLabelColor(ThemeJson themeJson)
    {
        return themeJson.fontColorLabels
            .OrIfDefault(themeJson.fontColorButtons)
            .OrIfDefault(Colors.CreateColor("#d0d0d0"));
    }
}
