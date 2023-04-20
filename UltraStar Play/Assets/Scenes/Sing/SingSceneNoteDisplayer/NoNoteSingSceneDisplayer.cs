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
    [Inject(UxmlName = R.UxmlNames.currentSentenceContainer)]
    private List<VisualElement> currentSentenceContainers;

    [Inject(UxmlName = R.UxmlNames.nextSentenceContainer)]
    private List<VisualElement> nextSentenceContainers;
    
    public override void OnInjectionFinished()
    {
        targetNoteEntryContainer.Clear();
        recordedNoteEntryContainer.Clear();
        effectsContainer.Clear();
        currentSentenceContainers.ForEach(it => it.HideByDisplay());
        nextSentenceContainers.ForEach(it => it.HideByDisplay());
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
