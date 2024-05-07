using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ThemeMetaUtilsTest
{
    private readonly string cyclicThemeName = "cyclic_theme";

    [Test]
    public void ShouldResolveTheme()
    {
        List<ThemeMeta> themeMetas = GetThemeMetas()
            .Where(themeMeta => themeMeta.FileNameWithoutExtension != cyclicThemeName)
            .ToList();
        ResolveThemeMetaUtils.ResolveThemes(themeMetas);

        ThemeMeta baseTheme = themeMetas.FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == "base_theme");
        ThemeMeta subTheme = themeMetas.FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == "sub_theme");
        ThemeMeta subSubTheme = themeMetas.FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == "sub_sub_theme");
        ThemeMeta themeInOtherFolder = themeMetas.FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == "theme_in_other_folder");

        Assert.NotNull(baseTheme);
        Assert.NotNull(subTheme);
        Assert.NotNull(subSubTheme);
        Assert.NotNull(themeInOtherFolder);

        // Check expected theme hierarchy
        Assert.IsNull(baseTheme.ThemeJson.parentTheme);
        Assert.AreEqual("base_theme", subTheme.ThemeJson.parentTheme);
        Assert.AreEqual("sub_theme", subSubTheme.ThemeJson.parentTheme);
        Assert.AreEqual("base_theme", themeInOtherFolder.ThemeJson.parentTheme);

        // Check expected properties of base theme
        Assert.AreEqual(Colors.CreateColor("#AAAAAA"), baseTheme.ThemeJson.primaryFontColor);
        Assert.AreEqual(Colors.CreateColor("#BBBBBB"), baseTheme.ThemeJson.secondaryFontColor);
        Assert.AreEqual(Colors.CreateColor("#CCCCCC"), baseTheme.ThemeJson.warningFontColor);
        Assert.AreEqual(Colors.CreateColor("#DDDDDD"), baseTheme.ThemeJson.errorFontColor);
        Assert.AreEqual(Colors.CreateColor("#EEEEEE"), baseTheme.ThemeJson.videoPreviewColor);
        Assert.AreEqual(
            new FileInfo($"{baseTheme.AbsoluteFolderPath}/some/path_relative_to/base_theme.png").FullName,
            new FileInfo(baseTheme.ThemeJson.staticBackground.imagePath).FullName);

        // Property value of the parent theme is used if needed
        Assert.AreEqual(Colors.CreateColor("#FF00FF"), subTheme.ThemeJson.primaryFontColor);
        Assert.AreEqual(Colors.CreateColor("#00FF00"), subTheme.ThemeJson.secondaryFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.warningFontColor, subTheme.ThemeJson.warningFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.errorFontColor, subTheme.ThemeJson.errorFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.videoPreviewColor, subTheme.ThemeJson.videoPreviewColor);
        Assert.AreEqual(baseTheme.ThemeJson.staticBackground.imagePath, subTheme.ThemeJson.staticBackground.imagePath);

        Assert.AreEqual(Colors.CreateColor("#777777"), subSubTheme.ThemeJson.primaryFontColor);
        Assert.AreEqual(subTheme.ThemeJson.secondaryFontColor, subSubTheme.ThemeJson.secondaryFontColor);
        Assert.AreEqual(subTheme.ThemeJson.warningFontColor, subSubTheme.ThemeJson.warningFontColor);
        Assert.AreEqual(subTheme.ThemeJson.errorFontColor, subSubTheme.ThemeJson.errorFontColor);
        Assert.AreEqual(Colors.CreateColor("#888888"), subSubTheme.ThemeJson.videoPreviewColor);
        Assert.AreEqual(subTheme.ThemeJson.staticBackground.imagePath, subSubTheme.ThemeJson.staticBackground.imagePath);

        Assert.AreEqual(baseTheme.ThemeJson.primaryFontColor, themeInOtherFolder.ThemeJson.primaryFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.secondaryFontColor, themeInOtherFolder.ThemeJson.secondaryFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.warningFontColor, themeInOtherFolder.ThemeJson.warningFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.errorFontColor, themeInOtherFolder.ThemeJson.errorFontColor);
        Assert.AreEqual(baseTheme.ThemeJson.videoPreviewColor, themeInOtherFolder.ThemeJson.videoPreviewColor);
        Assert.AreEqual(
            new FileInfo($"{themeInOtherFolder.AbsoluteFolderPath}/some/path_relative_to/theme_in_other_folder.png").FullName,
            new FileInfo(themeInOtherFolder.ThemeJson.staticBackground.imagePath).FullName);
    }

    private static List<ThemeMeta> GetThemeMetas()
    {
        string testThemeFolder = $"{Application.dataPath}/Editor/Tests/TestThemes/";
        List<string> themeFiles = DirectoryUtils.GetFiles(testThemeFolder, true, "*.json");
        List<ThemeMeta> themeMetas = themeFiles.Select(file => new ThemeMeta(file)).ToList();
        return themeMetas;
    }

    [Test]
    public void CannotResolveCyclicParentTheme()
    {
        LogAssert.Expect(LogType.Exception, new Regex(".*CyclicThemeReferenceException.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*Failed to resolve parent themes.*"));

        List<ThemeMeta> themeMetas = GetThemeMetas();
        ThemeMeta cyclicTheme = themeMetas.FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == cyclicThemeName);
        ResolveThemeMetaUtils.ResolveThemes(new List<ThemeMeta>() { cyclicTheme });
    }
}
