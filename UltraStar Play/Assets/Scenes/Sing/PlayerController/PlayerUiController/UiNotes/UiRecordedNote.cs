using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiRecordedNote : MonoBehaviour
{
    private Image image;

    void Awake()
    {
        image = GetComponentInChildren<Image>();
    }

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        // Set hue of HSV shader to the hue of the color from the MicProfile.
        HsvColor hsvColor = new HsvColor(micProfile.Color);
        // In the shader, the red channel of the color is interpreted as hue.
        image.color = new Color(hsvColor.H, 0, 0);
    }
}
