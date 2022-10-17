
using System;
using UnityEngine;

[Serializable]
public class GraphicSettings
{
    // Screen.currentResolution may only be called from Start() and Awake(), thus use a dummy here.
    public ScreenResolution resolution = new(800, 600, 60);
    public FullScreenMode fullScreenMode = FullScreenMode.Windowed;
    public int targetFps = 30;
    public ENoteDisplayMode noteDisplayMode = ENoteDisplayMode.SentenceBySentence;
    public bool showPitchIndicator;
    public bool useImageAsCursor = true;
    public bool showLyricsOnNotes;
    public bool analyzeBeatsWithoutTargetNote = true;
    public string themeName = "BaseTheme";
    public bool AnimateSceneChange { get; set; } = true;

    string currentThemeName = ThemeManager.DEFAULT_THEME;
    public string CurrentThemeName
    {
        get => currentThemeName;
        set
        {
            if (SettingsManager.Instance.Settings.DeveloperSettings.disableDynamicThemes)
                return;

            currentThemeName = value;
            ThemeManager.Instance.LoadTheme(currentThemeName);
        }
    }
}
