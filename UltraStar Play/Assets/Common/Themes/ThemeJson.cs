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
    public DynamicBackgroundJson dynamicBackground;
    public SongRatingIconsJson songRatingIcons;
    
    public Color32 backgroundColorButtons;
    public Color32 fontColorButtons;
    
    public Color32 fontColorLabels;
    
    public List<Color32> microphoneColors;
    public Dictionary<string, Color32> phraseRatingColors;
    public Dictionary<string, Color32> songEditorLayerColors;
    
    public string backgroundMusic;
    
    public Color32 lyricsContainerColor;
    public Color32 lyricsColor;
    public Color32 lyricsOutlineColor;
    public Color32 currentNoteLyricsColor;
    public Color32 previousNoteLyricsColor;

    /**
     * Time with unit for scene transition, e.g. "0.5s" or "200 ms".
     */
    public string sceneTransitionAnimationTime;
}
