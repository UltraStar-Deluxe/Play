using System;
using UnityEngine;

[Serializable]
public class DynamicBackground
{
    public enum GradientType
    {
        Radial = 0,
        RadialRepeated = 1,
        Linear = 2,
        Reflected = 3,
        Repeated = 4
    }

    // Material
    public string gradientRampFile = null;
    public string gradientType = "Radial";
    public float gradientScrollingSpeed = 0;
    public float gradientScale = 1.0f;
    public float gradientSmoothness = 1.0f;
    public float gradientAngle = 0.0f;
    public bool gradientAnimation = false;
    public float gradientAnimSpeed = 1.0f;
    public float gradientAnimAmplitude = 0.1f;
    public string patternFile;
    public Color patternColor = Color.white;
    public Vector2 patternScale = Vector2.one;
    public Vector2 patternScrolling = Vector2.zero;
    public float uiShadowOpacity = 0;
    public Vector2 uiShadowOffset;
    // Particles
    public string particleFile = null;
    public float particleOpacity = 0f;
    // TODO particle movement pattern, based on an enum that will correspond to different prefabs
}
