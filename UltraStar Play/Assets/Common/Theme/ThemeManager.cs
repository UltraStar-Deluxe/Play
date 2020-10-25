using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        themeNameToTheme?.Clear();
    }

    public static readonly string themesFolderName = "Themes";
    public static readonly string themesFileName = "Themes.xml";
    public static readonly string colorsFileName = "Colors.properties";

    public static ThemeManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ThemeManager>("ThemeManager");
        }
    }

    // Field is static to be persisted across scenes.
    private static Dictionary<string, Theme> themeNameToTheme;

    private static Theme currentTheme;
    public static Theme CurrentTheme
    {
        get
        {
            return currentTheme;
        }
        set
        {
            if (value != null)
            {
                currentTheme = value;
                ThemeManager.Instance.currentThemeName = currentTheme.Name;
            }
            else
            {
                Debug.LogError("Trying to set CurrentTheme to null");
            }
        }
    }

    public bool logInfo;

    [ReadOnly]
    public string currentThemeName;

    private CoroutineManager coroutineManager;

    private void Awake()
    {
        if (themeNameToTheme.IsNullOrEmpty())
        {
            coroutineManager = CoroutineManager.Instance;
            ReloadThemes();
        }
    }

#if UNITY_EDITOR
    private void Start()
    {
        // Awake is not called normally after re-compile.
        Awake();
    }

    private void Update()
    {
        if (coroutineManager == null)
        {
            coroutineManager = CoroutineManager.Instance;
        }

        if (themeNameToTheme == null)
        {
            ReloadThemes();
        }
    }
#endif

    public void UpdateThemeResources()
    {
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

    /**
     * Loads an AudioClip from the current theme.
     * The path is relative to the theme folder.
     */
    public static AudioClip GetAudioClip(string path)
    {
        return CurrentTheme.LoadAudioClip(path);
    }

    /**
     * Loads a Sprite from the current theme.
     * The path is relative to the theme folder.
     */
    public static Sprite GetSprite(string path)
    {
        return CurrentTheme.LoadSprite(path);
    }

    /**
    * Loads a Color from the current theme.
    */
    public static Color32 GetColor(string colorName)
    {
        if (CurrentTheme.TryFindColor(colorName, out Color32 result))
        {
            return result;
        }
        Debug.LogWarning("Color not found: " + colorName);
        return Colors.white;
    }

    public static List<Theme> GetThemes()
    {
        return themeNameToTheme.Values.ToList();
    }

    public static Theme GetTheme(string themeName)
    {
        themeNameToTheme.TryGetValue(themeName, out Theme resultTheme);
        return resultTheme;
    }

    public void ReloadThemes()
    {
        themeNameToTheme = new Dictionary<string, Theme>();
        string themesFilePath = ApplicationUtils.GetStreamingAssetsPath(themesFolderName + "/" + themesFileName);
        string themesFileContent = File.ReadAllText(themesFilePath);
        ReloadThemesFromXml(themesFileContent);
    }

    private void ReloadThemesFromXml(string xml)
    {
        Dictionary<string, string> themeNameToParentThemeNameMap = new Dictionary<string, string>();

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

        // Use theme from settings
        string themeNameFromSettings = SettingsManager.Instance.Settings.GraphicSettings.themeName;
        if (themeNameToTheme.TryGetValue(themeNameFromSettings, out Theme themeFromSettings))
        {
            CurrentTheme = themeFromSettings;
        }
        else
        {
            CurrentTheme = themeNameToTheme.Values.FirstOrDefault();
        }

        if (logInfo)
        {
            string themeNamesCsv = string.Join(", ", themeNameToTheme.Keys.ToList());
            Debug.Log($"Loaded themes: [{themeNamesCsv}]");
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
