using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * Resolves relative file paths and parent themes.
 */
public static class ResolveThemeMetaUtils
{
    public static void ResolveThemes(List<ThemeMeta> allThemeMetas)
    {
        HashSet<string> resolvedThemes = new HashSet<string>();
        foreach (ThemeMeta themeMeta in allThemeMetas)
        {
            try
            {
                HashSet<string> seenParentNames = new HashSet<string>();
                ResolveThemeJsonRecursively(allThemeMetas, themeMeta, seenParentNames, resolvedThemes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to resolve parent themes of '{themeMeta.FileNameWithoutExtension}': {ex.Message}");
            }
        }
    }

    private static void ResolveThemeJsonRecursively(
        List<ThemeMeta> allThemeMetas,
        ThemeMeta themeMeta,
        HashSet<string> seenParentNames,
        HashSet<string> resolvedThemes)
    {
        string themeName = themeMeta.FileNameWithoutExtension;
        ThemeJson themeJson = themeMeta.ThemeJson;
        if (themeJson == null
            || resolvedThemes.Contains(themeName))
        {
            // Nothing to do
            return;
        }

        if (seenParentNames.Contains(themeJson.parentTheme))
        {
            throw new CyclicThemeReferenceException(
                $"There is a loop in the theme hierarchy involving parent themes '{seenParentNames.JoinWith(", ")}'.");
        }
        seenParentNames.Add(themeName);

        ThemeJson resolvedThemeJson;
        if (!themeJson.parentTheme.IsNullOrEmpty())
        {
            // Resolve parent data
            ThemeMeta parentThemeMeta = allThemeMetas.FirstOrDefault(it => it.FileNameWithoutExtension == themeJson.parentTheme);
            if (parentThemeMeta == null)
            {
                throw new ParentThemeNotFoundException(
                    $"Theme not found with name '{themeJson.parentTheme}'. " +
                    $"Available themes: {allThemeMetas.Select(availableThemeMeta => availableThemeMeta.FileNameWithoutExtension).JoinWith(", ")}");
            }

            ResolveThemeJsonRecursively(allThemeMetas, parentThemeMeta, seenParentNames, resolvedThemes);
            resolvedThemeJson = CopyThemeJson(parentThemeMeta.ThemeJson);

            // Apply own data
            JsonConverter.FillFromJson(themeMeta.FileContent, resolvedThemeJson);
        }
        else
        {
            resolvedThemeJson = CopyThemeJson(themeJson);
        }

        ResolveRelativeFilePaths(themeMeta.AbsoluteFolderPath, resolvedThemeJson);
        themeMeta.ThemeJson = resolvedThemeJson;

        resolvedThemes.Add(themeName);
    }

    private static ThemeJson CopyThemeJson(ThemeJson themeJson)
    {
        return JsonConverter.FromJson<ThemeJson>(JsonConverter.ToJson(themeJson));
    }

    private static void ResolveRelativeFilePaths(string absoluteFolderPath, ThemeJson themeJson)
    {
        if (themeJson == null)
        {
            return;
        }

        // TODO: Define file path as type, use it in the ThemeJson via custom JSON deserializer, then use reflection here to resolve file path properties
        ResolveRelativeFilePath(absoluteFolderPath,
            () => themeJson.backgroundMusic,
            newValue => themeJson.backgroundMusic = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => themeJson.beforeLyricsIndicatorImage,
            newValue => themeJson.beforeLyricsIndicatorImage = newValue);

        // Resolve paths in static and dynamic backgrounds
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.staticBackground);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.dynamicBackground);
        themeJson.sceneSpecificBackgrounds?.ForEach(entry =>
        {
            ResolveRelativeFilePaths(absoluteFolderPath, entry.Value?.staticBackground);
            ResolveRelativeFilePaths(absoluteFolderPath, entry.Value?.dynamicBackground);
        });

        // Resolve paths in SongRatingIcons
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.songRatingIcons);

        // Resolve paths in ControlStyleConfigs
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.defaultControl);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.transparentButton);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.textOnlyButton);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.dangerButton);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.slideToggleOff);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.defaultControl);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.slideToggleOn);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.dynamicPanel);
        ResolveRelativeFilePaths(absoluteFolderPath, themeJson.staticPanel);
    }

    private static void ResolveRelativeFilePaths(string absoluteFolderPath, ControlStyleConfig controlStyleConfig)
    {
        if (controlStyleConfig == null)
        {
            return;
        }

        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.backgroundImage,
            newValue => controlStyleConfig.backgroundImage = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.hoverBackgroundImage,
            newValue => controlStyleConfig.hoverBackgroundImage = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.focusBackgroundImage,
            newValue => controlStyleConfig.focusBackgroundImage = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.activeBackgroundImage,
            newValue => controlStyleConfig.activeBackgroundImage = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.hoverActiveBackgroundImage,
            newValue => controlStyleConfig.hoverActiveBackgroundImage = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.hoverFocusBackgroundImage,
            newValue => controlStyleConfig.hoverFocusBackgroundImage = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => controlStyleConfig.disabledBackgroundImage,
            newValue => controlStyleConfig.disabledBackgroundImage = newValue);
    }

    private static void ResolveRelativeFilePaths(string absoluteFolderPath, SongRatingIconsJson songRatingIcons)
    {
        if (songRatingIcons == null)
        {
            return;
        }

        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.toneDeaf,
            newValue => songRatingIcons.toneDeaf = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.amateur,
            newValue => songRatingIcons.amateur = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.wannabe,
            newValue => songRatingIcons.wannabe = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.hopeful,
            newValue => songRatingIcons.hopeful = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.risingStar,
            newValue => songRatingIcons.risingStar = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.leadSinger,
            newValue => songRatingIcons.leadSinger = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.superstar,
            newValue => songRatingIcons.superstar = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => songRatingIcons.ultrastar,
            newValue => songRatingIcons.ultrastar = newValue);
    }

    private static void ResolveRelativeFilePaths(string absoluteFolderPath, StaticBackgroundJson staticBackground)
    {
        if (staticBackground == null)
        {
            return;
        }

        ResolveRelativeFilePath(absoluteFolderPath,
            () => staticBackground.imagePath,
            newValue => staticBackground.imagePath = newValue);
    }

    private static void ResolveRelativeFilePaths(string absoluteFolderPath, DynamicBackgroundJson dynamicBackground)
    {
        if (dynamicBackground == null)
        {
            return;
        }

        ResolveRelativeFilePath(absoluteFolderPath,
            () => dynamicBackground.gradientRampFile,
            newValue => dynamicBackground.gradientRampFile = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => dynamicBackground.patternFile,
            newValue => dynamicBackground.patternFile = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => dynamicBackground.particleFile,
            newValue => dynamicBackground.particleFile = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => dynamicBackground.videoPath,
            newValue => dynamicBackground.videoPath = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => dynamicBackground.lightVideoPath,
            newValue => dynamicBackground.lightVideoPath = newValue);
        ResolveRelativeFilePath(absoluteFolderPath,
            () => dynamicBackground.imagePath,
            newValue => dynamicBackground.imagePath = newValue);
    }

    private static void ResolveRelativeFilePath(string absoluteFolderPath, Func<string> filePathGetter, Action<string> filePathSetter)
    {
        string filePath = filePathGetter();
        if (filePath.IsNullOrEmpty())
        {
            return;
        }

        string absoluteFilePath = PathUtils.GetAbsoluteFilePath(absoluteFolderPath, filePath);
        if (absoluteFilePath != filePath)
        {
            filePathSetter(absoluteFilePath);
        }
    }
}
