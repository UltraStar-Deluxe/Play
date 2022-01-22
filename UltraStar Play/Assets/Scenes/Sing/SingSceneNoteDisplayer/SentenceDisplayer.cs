using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SentenceDisplayer : AbstractSingSceneNoteDisplayer
{
    [Inject]
    private PlayerControl playerControl;

    [Inject(UxmlName = R.UxmlNames.lyricsContainer)]
    private VisualElement lyricsContainer;

    private Sentence currentSentence;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        lyricsContainer.HideByDisplay();
        playerControl.EnterSentenceEventStream.Subscribe(enterSentenceEvent =>
        {
            DisplaySentence(enterSentenceEvent.Sentence);
        });
    }

    private void DisplaySentence(Sentence sentence)
    {
        currentSentence = sentence;
        RemoveAllDisplayedNotes();
        if (sentence == null)
        {
            return;
        }

        avgMidiNote = currentSentence.Notes.Count > 0
            ? (int)currentSentence.Notes.Select(it => it.MidiNote).Average()
            : 0;
        // The division is rounded down on purpose (e.g. noteRowCount of 3 will result in (noteRowCount / 2) == 1)
        maxNoteRowMidiNote = avgMidiNote + (noteRowCount / 2);
        minNoteRowMidiNote = avgMidiNote - (noteRowCount / 2);
        // Freestyle notes are not drawn
        IEnumerable<Note> nonFreestyleNotes = sentence.Notes.Where(note => !note.IsFreestyle);
        foreach (Note note in nonFreestyleNotes)
        {
            CreateTargetNoteControl(note);
        }
    }

    protected override void DisplayRecordedNote(RecordedNote recordedNote)
    {
        if (currentSentence == null
            || recordedNote.TargetSentence != currentSentence
            || (recordedNote.TargetNote != null && recordedNote.TargetNote.Sentence != currentSentence))
        {
            // This is probably a recorded note from the previous sentence that is still continued because of the mic delay.
            // Do not draw the recorded note, it is not in the displayed sentence.
            return;
        }

        base.DisplayRecordedNote(recordedNote);
    }

    protected override void UpdateNotePosition(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        int sentenceStartBeat = currentSentence.MinBeat;
        int sentenceEndBeat = currentSentence.MaxBeat;
        int beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        Vector2 yStartEndPercent = GetYStartAndEndInPercentForMidiNote(midiNote);
        float yStartPercent = yStartEndPercent.x;
        float yEndPercent = yStartEndPercent.y;
        float xStartPercent = (float)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        float xEndPercent = (float)(noteEndBeat - sentenceStartBeat) / beatsInSentence;

        yStartPercent *= 100;
        yEndPercent *= 100;
        xStartPercent *= 100;
        xEndPercent *= 100;

        visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
        visualElement.style.left = new StyleLength(new Length(xStartPercent, LengthUnit.Percent));
        visualElement.style.width = new StyleLength(new Length(xEndPercent - xStartPercent, LengthUnit.Percent));
        visualElement.style.bottom = new StyleLength(new Length(yStartPercent, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(yEndPercent - yStartPercent, LengthUnit.Percent));
    }
}
