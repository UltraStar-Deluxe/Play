using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class StaticBackgroundJson
{
    /**
     * Image file path
     */
    public string imagePath;

    /**
     * Video file path
     */
    public string videoPath;
    
    /**
     * Defines how the image element is scaled to fit the screen. One of ScaleMode enum values.
     */
    public string imageScaleMode;

    /**
     * Defines how the video element is scaled to fit the screen. One of ScaleMode enum values.
     */
    public string videoScaleMode;
    
    /**
     * The playback speed for the video.
     */
    public float videoPlaybackSpeed;
}
