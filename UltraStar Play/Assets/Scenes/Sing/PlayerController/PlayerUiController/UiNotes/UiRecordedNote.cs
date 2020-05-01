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

    public RecordedNote RecordedNote { get; set; }

    // The end beat is a double here, in contrast to the RecordedNote.
    // This is because the UiRecordedNote is drawn smoothly from start to end of the RecordedNote using multiple frames.
    // Therefor, the resolution of start and end for UiRecordedNotes must be more fine grained than whole beats.
    public int StartBeat { get; set; }
    public double EndBeat { get; set; }
    public int TargetEndBeat { get; set; }
    public float LifeTimeInSeconds { get; set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        LifeTimeInSeconds += Time.deltaTime;
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
