using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMicPitchIndicatorControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.micPitchOutOfRangeIndicatorTop)]
    private VisualElement micPitchOutOfRangeIndicatorTop;

    [Inject(UxmlName = R.UxmlNames.micPitchOutOfRangeIndicatorBottom)]
    private VisualElement micPitchOutOfRangeIndicatorBottom;

    [Inject]
    private Settings settings;

    [Inject]
    private NoteAreaControl noteAreaControl;

    public void OnInjectionFinished()
    {
        micPitchOutOfRangeIndicatorTop.HideByDisplay();
        micPitchOutOfRangeIndicatorBottom.HideByDisplay();
    }
}
