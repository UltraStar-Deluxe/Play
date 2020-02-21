using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiRecordedNote : MonoBehaviour
{
    private ImageHueHelper imageHueHelper;

    void Awake()
    {
        imageHueHelper = GetComponentInChildren<ImageHueHelper>();
    }

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        if (micProfile != null)
        {
            imageHueHelper.SetHueByColor(micProfile.Color);
        }
    }
}
