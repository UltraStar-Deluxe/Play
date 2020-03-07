using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

public class UiRecordedNote : MonoBehaviour
{
    [InjectedInInspector]
    public Text lyricsUiText;
    [InjectedInInspector]
    public ImageHueHelper imageHueHelper;

    public RectTransform RectTransform { get; private set; }
    public int MidiNote { get; set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        if (micProfile != null)
        {
            SetColor(micProfile.Color);
        }
    }

    public void SetColor(Color color)
    {
        imageHueHelper.SetHueByColor(color);
    }
}
