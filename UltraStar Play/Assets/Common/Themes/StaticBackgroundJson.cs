using System;
using UnityEngine;

[Serializable]
public class StaticBackgroundJson
{
    /**
     * Image or video file path
     */
    public string imagePath;

    /**
     * One of ScaleMode enum values.
     */
    public string scaleMode;

    /**
     * If a video file is used, determines the playback speed for the video.
     */
    public float playbackSpeed;
}
