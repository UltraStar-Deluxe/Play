using System;
using UnityEngine;

[Serializable]
public class DynamicBackgroundJson
{
    // Material
    public string gradientRampFile;
    public string gradientType = "Radial";
    public float gradientScrollingSpeed;
    public float gradientScale = 1.0f;
    public float gradientSmoothness = 1.0f;
    public float gradientAngle;
    public bool gradientAnimation;
    public float gradientAnimSpeed = 1.0f;
    public float gradientAnimAmplitude = 0.1f;
    public string patternFile;
    public Color32 patternColor = Colors.white;
    public Vector2 patternScale = Vector2.one;
    public Vector2 patternScrolling = Vector2.zero;
    public float uiShadowOpacity;
    public Vector2 uiShadowOffset;
    public string particleFile;
    public float particleOpacity;
    
    /**
     * Video for base background.
     */
    public string videoPath;
    public float videoPlaybackSpeed;
    
    /**
     * Image for static base background.
     */
    public string imagePath;
    
    /**
     * Video for additive light.
     */
    public string lightVideoPath;
    public float lightVideoPlaybackSpeed;
}
