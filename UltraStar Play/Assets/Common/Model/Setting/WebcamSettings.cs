using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WebcamSettings
{
    public string CurrentDeviceName { get; set; }
    public bool UseAsBackgroundInSingScene { get; set; }
}
