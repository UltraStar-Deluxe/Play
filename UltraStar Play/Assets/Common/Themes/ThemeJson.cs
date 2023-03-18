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
    public StaticBackgroundJson staticBackground;
    public Dictionary<string, StaticBackgroundJson> sceneSpecificStaticBackgrounds;
    public DynamicBackgroundJson dynamicBackground;
    public SongRatingIconsJson songRatingIcons;
    
    public ControlStyleConfig defaultControl;
    public ControlStyleConfig transparentButton;
    public ControlStyleConfig textOnlyButton;
    public ControlStyleConfig lightButton;
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
