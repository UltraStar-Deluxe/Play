using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        themeNameToTheme.Clear();
    }

    public static readonly string themesFolderName = "Themes";
    public static readonly string themesFileBaseName = "Themes";
    public static readonly string colorsFileBaseName = "Colors";

    // Field is static to be persisted across scenes.
    private static readonly Dictionary<string, Theme> themeNameToTheme = new Dictionary<string, Theme>();

    public static ThemeManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ThemeManager>("ThemeManager");
        }
    }

    public bool logInfo;

    [ReadOnly]
    public string currentThemeName;

    public Theme currentTheme;
    public Theme CurrentTheme
    {
        get
        {
            if (themeNameToTheme.IsNullOrEmpty())
            {
                ReloadThemes();
            }
            return currentTheme;
        }
        set
        {
            if (value != null)
            {
                currentTheme = value;
                currentThemeName = currentTheme.Name;
            }
            else
            {
                Debug.LogError("Trying to set CurrentTheme to null");
            }
        }
    }

    public void UpdateThemeResources()
    {
        ReloadThemes();

        if (CurrentTheme == null)
        {
            return;
        }

        Themeable[] themables = FindObjectsOfType<Themeable>(true);
        Debug.Log($"Updating {themables.Length} Themeable instances in scene");
        foreach (Themeable themeable in themables)
        {
            themeable.ReloadResources(CurrentTheme);
        }
    }

    public List<string> GetLoadedThemeNames()
    {
        return themeNameToTheme.Keys.ToList();
    }

    public Theme GetTheme(string themeName)
    {
        themeNameToTheme.TryGetValue(themeName, out Theme resultTheme);
        return resultTheme;
    }

    public void ReloadThemes()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        Dictionary<string, string> themeNameToParentThemeNameMap = new Dictionary<string, string>();

        TextAsset themesTextAsset = Resources.Load<TextAsset>(themesFolderName + "/" + themesFileBaseName);
        string xml = themesTextAsset.text;
        XElement xthemes = XElement.Parse(xml);
        foreach (XElement xtheme in xthemes.Elements("theme"))
        {
            string name = xtheme.Attribute("name").String();
            string parentName = xtheme.Attribute("parent").String();
            themeNameToParentThemeNameMap.Add(name, parentName);
        }

        themeNameToTheme.Clear();
        foreach (string themeName in themeNameToParentThemeNameMap.Keys)
        {
            GetOrCreateAndAddTheme(themeName, themeNameToParentThemeNameMap);
        }

        CurrentTheme = themeNameToTheme.Values.FirstOrDefault();

        stopwatch.Stop();

        if (logInfo)
        {
            string themeNamesCsv = string.Join(", ", GetLoadedThemeNames());
            Debug.Log($"Loaded themes: [{themeNamesCsv}] in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private Theme GetOrCreateAndAddTheme(string themeName, Dictionary<string, string> themeNameToParentThemeNameMap)
    {
        // Check if there is already a theme with this name
        Theme existingTheme = GetTheme(themeName);
        if (existingTheme != null)
        {
            return existingTheme;
        }

        // No theme with this name has been found. Thus, create a new one and add it to the list of themes.
        Theme newTheme;
        themeNameToParentThemeNameMap.TryGetValue(themeName, out string parentThemeName);
        if (string.IsNullOrEmpty(parentThemeName))
        {
            newTheme = new Theme(themeName, null);
        }
        else
        {
            Theme parentTheme = GetOrCreateAndAddTheme(parentThemeName, themeNameToParentThemeNameMap);
            newTheme = new Theme(themeName, parentTheme);
        }
        themeNameToTheme.Add(newTheme.Name, newTheme);
        return newTheme;
    }
}
