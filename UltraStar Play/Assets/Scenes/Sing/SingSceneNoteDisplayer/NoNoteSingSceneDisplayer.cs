using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoNoteSingSceneDisplayer : AbstractSingSceneNoteDisplayer
{
    [Inject(UxmlName = R.UxmlNames.lyricsContainer)]
    private VisualElement lyricsContainer;

    public override void OnInjectionFinished()
    {
        targetNoteEntryContainer.Clear();
        recordedNoteEntryContainer.Clear();
        effectsContainer.Clear();
        lyricsContainer.HideByDisplay();
    }

    protected override void UpdateTargetNoteControl(TargetNoteControl targetNoteControl, int indexInList)
    {
        // Do nothing.
    }

    protected override Rect GetNotePositionInPercent(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        return Rect.zero;
    }
}
