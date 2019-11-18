using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeManager : MonoBehaviour
{
    public string currentThemeName;

    // Themes is static to be persisted across scenes.
    private static readonly List<Theme> themes = new List<Theme>();

    public static ThemeManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ThemeManager>("ThemeManager");
        }
    }

    void OnEnable()
    {
        if (themes.IsNullOrEmpty())
        {
            ReloadThemes();
        }
    }

    public void UpdateThemeResources()
    {
        ReloadThemes();

        if (GetCurrentTheme() == null)
        {
            return;
        }

        Themeable[] themables = FindObjectsOfType<Themeable>();
        Debug.Log($"Updating {themables.Length} Themeable instances in scene");
        foreach (Themeable themeable in themables)
        {
            themeable.ReloadResources(GetCurrentTheme());
        }
    }

    public List<string> GetLoadedThemeNames()
    {
        return themes.Select(it => it.Name).ToList();
    }

    public Theme GetCurrentTheme()
    {
        Theme currentTheme = GetTheme(currentThemeName);
        if (currentTheme == null)
        {
            throw new Exception($"Current theme {currentThemeName} does not exist: ");
        }
        return currentTheme;
    }

    public Theme GetTheme(string themeName)
    {
        return themes.Where(it => it.Name == themeName).FirstOrDefault();
    }

    public void ReloadThemes()
    {
        Dictionary<string, string> themeNameToParentThemeNameMap = new Dictionary<string, string>();

        TextAsset themesTextAsset = Resources.Load<TextAsset>("themes");
        string xml = themesTextAsset.text;
        XElement xthemes = XElement.Parse(xml);
        foreach (XElement xtheme in xthemes.Elements("theme"))
        {
            string name = xtheme.Attribute("name").String();
            string parentName = xtheme.Attribute("parent").String();
            themeNameToParentThemeNameMap.Add(name, parentName);
        }

        themes.Clear();
        foreach (string themeName in themeNameToParentThemeNameMap.Keys)
        {
            GetOrCreateAndAddTheme(themeName, themeNameToParentThemeNameMap);
        }

        string themeNamesCsv = string.Join(", ", themes.Select(it => it.ToString()));
        Debug.Log("Loaded themes: " + themeNamesCsv);
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
        themes.Add(newTheme);
        return newTheme;
    }
}
