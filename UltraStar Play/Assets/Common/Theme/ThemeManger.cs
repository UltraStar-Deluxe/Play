using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeManger : MonoBehaviour
{
    public string currentThemeName;

    private readonly List<Theme> themes = new List<Theme>();

    public static ThemeManger Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ThemeManger>("ThemeManager");
        }
    }

    void OnEnable()
    {
        ReloadThemes();
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
            themeable.ReloadResources();
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
        themes.Clear();

        TextAsset themesTextAsset = Resources.Load<TextAsset>("themes");
        string xml = themesTextAsset.text;
        XElement xthemes = XElement.Parse(xml);
        foreach (XElement xtheme in xthemes.Elements("theme"))
        {
            string name = xtheme.Attribute("name").String();
            string parentName = xtheme.Attribute("parent").String();
            Theme theme = new Theme(name, parentName);
            themes.Add(theme);
        }

        string themeNamesCsv = string.Join(", ", themes.Select(it => it.ToString()));
        Debug.Log("Loaded themes: " + themeNamesCsv);
    }
}
