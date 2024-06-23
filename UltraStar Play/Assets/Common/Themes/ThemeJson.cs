using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * Data structure for theme files.
 * The fields correspond to the possible properties defined in a JSON theme file.
 */
[Serializable]
public class ThemeJson
{
    public string parentTheme;

    public List<string> styleSheets;

    public StaticBackgroundJson staticBackground;
    public Dictionary<string, StaticAndDynamicBackgroundJson> sceneSpecificBackgrounds;
    public DynamicBackgroundJson dynamicBackground;
    public SongRatingIconsJson songRatingIcons;

    public ControlStyleConfig defaultControl;
    public ControlStyleConfig transparentButton;
    public ControlStyleConfig textOnlyButton;
    public ControlStyleConfig dangerButton;
    public ControlStyleConfig toggle;
    public ControlStyleConfig slideToggleOff;
    public ControlStyleConfig slideToggleOn;
    public ControlStyleConfig dynamicPanel;
    public ControlStyleConfig staticPanel;

    public Color32 primaryFontColor;
    public Color32 secondaryFontColor;
    public Color32 warningFontColor;
    public Color32 errorFontColor;
    public TextShadowConfig noBackgroundInHierarchyTextShadow;

    public List<Color32> microphoneColors;
    public Dictionary<string, Color32> phraseRatingColors;
    public Dictionary<string, Color32> songEditorLayerColors;
    public Color32 videoPreviewColor;

    public string backgroundMusic;

    public GradientConfig lyricsContainerGradient;
    public string beforeLyricsIndicatorImage;
    public bool lyricsShadow = true;
    public Color32 lyricsColor;
    public Color32 nextLyricsColor;
    public Color32 lyricsOutlineColor;
    public Color32 currentNoteLyricsColor;
    public Color32 previousNoteLyricsColor;
    public Color32 goldenColor;
}
