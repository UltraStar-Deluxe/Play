using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiRecordedNote : MonoBehaviour
{
    public float colorFactor = 0.6f;

    private Image image;

    void Awake()
    {
        image = GetComponentInChildren<Image>();
    }

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        float r = micProfile.Color.r * colorFactor;
        float g = micProfile.Color.g * colorFactor;
        float b = micProfile.Color.b * colorFactor;
        image.color = new Color(r, g, b, 1);
    }
}
