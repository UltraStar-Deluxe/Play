using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/**
 * Data structure for theme files.
 * The fields correspond to the possible properties defined in a JSON theme file.
 */
[Serializable]
public class ThemeJson
{
    public StaticBackgroundJson staticBackground;
    public Dictionary<string, StaticBackgroundJson> sceneSpecificStaticBackgrounds;
    public DynamicBackgroundJson dynamicBackground;
    public SongRatingIconsJson songRatingIcons;
    
    public GradientConfig buttonBackgroundGradient;
    public GradientConfig hoverButtonBackgroundGradient;
    public GradientConfig focusButtonBackgroundGradient;
    public GradientConfig activeButtonBackgroundGradient;
    public Color32 backgroundColorButtons;
    public Color32 fontColorButtons;
    
    public Color32 primaryFontColor;
    public Color32 secondaryFontColor;
    public Color32 warningFontColor;
    public Color32 errorFontColor;
    
    public List<Color32> microphoneColors;
    public Dictionary<string, Color32> phraseRatingColors;
    public Dictionary<string, Color32> songEditorLayerColors;
    
    public string backgroundMusic;
    
    public GradientConfig lyricsContainerGradient;
    public Color32 lyricsColor;
    public Color32 lyricsOutlineColor;
    public Color32 currentNoteLyricsColor;
    public Color32 previousNoteLyricsColor;

    /**
     * Time with unit for scene transition, e.g. "0.5s" or "200 ms".
     */
    public string sceneTransitionAnimationTime;
}
