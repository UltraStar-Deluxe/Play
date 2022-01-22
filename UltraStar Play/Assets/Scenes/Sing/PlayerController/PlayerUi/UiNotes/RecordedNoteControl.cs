using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UnityEngine.UIElements;

public class RecordedNoteControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.targetNote)]
    private VisualElement targetNote;

    [Inject(UxmlName = R.UxmlNames.recordedNoteImage)]
    private VisualElement backgroundImage;

    [Inject(UxmlName = R.UxmlNames.recordedNoteLabel)]
    public Label Label { get; private set; }

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    public RecordedNote RecordedNote { get; private set; }

    public int MidiNote { get; set; }

    // The end beat is a double here, in contrast to the RecordedNote.
    // This is because the UiRecordedNote is drawn smoothly from start to end of the RecordedNote using multiple frames.
    // Therefor, the resolution of start and end for UiRecordedNotes must be more fine grained than whole beats.
    public int StartBeat { get; set; }
    public double EndBeat { get; set; }
    public int TargetEndBeat { get; set; }
    public float LifeTimeInSeconds { get; set; }

    public void OnInjectionFinished()
    {
        targetNote.HideByDisplay();

        if (micProfile != null)
        {
            SetStyleByMicProfile(micProfile);
        }
    }

    public void Update()
    {
        LifeTimeInSeconds += Time.deltaTime;
    }

    private void SetStyleByMicProfile(MicProfile micProfile)
    {
        // If no target note, then remove saturation from color and make transparent
        Color color = micProfile.Color;
        Color finalColor = (RecordedNote != null && RecordedNote.TargetNote == null)
            ? color.RgbToHsv().WithGreen(0).HsvToRgb().WithAlpha(0.25f)
            : color;
        backgroundImage.style.unityBackgroundImageTintColor = finalColor;
    }

    public void Dispose()
    {
        VisualElement.RemoveFromHierarchy();
    }
}
