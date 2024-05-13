using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SentenceDisplayer : AbstractSingSceneNoteDisplayer
{
    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private Sentence currentSentence;

    private double maxMicDelayInMillis = -1;
    private double MaxMicDelayInMillis
    {
        get
        {
            if (maxMicDelayInMillis < 0
                && !singSceneControl.PlayerControls.IsNullOrEmpty())
            {
                List<int> micDelays = singSceneControl.PlayerControls
                    .Where(it => it.MicProfile != null)
                    .Select(it => it.MicProfile.DelayInMillis)
                    .ToList();
                maxMicDelayInMillis = !micDelays.IsNullOrEmpty() ? micDelays.Max() : 0;
                return maxMicDelayInMillis;
            }

            return maxMicDelayInMillis;
        }
    }
    private PlayerControl.EnterSentenceEvent lastEnterSentenceEvent;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        playerControl.EnterSentenceEventStream
            .Subscribe(evt => OnEnterSentence(evt))
            .AddTo(gameObject);
    }

    private void OnEnterSentence(PlayerControl.EnterSentenceEvent evt)
    {
        lastEnterSentenceEvent = evt;
        Sentence sentence = evt.Sentence;

        // Delay displaying next sentence for the mic delay if possible.
        double delayInMillis = 0;
        if (sentence != null)
        {
            double positionInMillis = songAudioPlayer.PositionInMillis;
            double durationInMillisUntilSentenceStart = SongMetaBpmUtils.BeatsToMillis(songMeta, sentence.MinBeat) - positionInMillis;
            delayInMillis = Math.Min(durationInMillisUntilSentenceStart, MaxMicDelayInMillis);
        }

        if (delayInMillis > 0)
        {
            float delayInSeconds = (float)(delayInMillis / 1000);
            MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(
                delayInSeconds, () =>
                {
                    // Only show the sentence if there was no other event in the meantime.
                    if (lastEnterSentenceEvent == evt)
                    {
                        DisplaySentence(sentence);
                    }
                }));
        }
        else
        {
            DisplaySentence(sentence);
        }
    }

    private void DisplaySentence(Sentence sentence)
    {
        if (sentence == null)
        {
            // Last sentence done.
            // Wait until the mic has finished recording the last note.
            // Afterwards, fade out notes, then remove notes.
            if (playerControl != null)
            {
                playerControl.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(1f,
                    () =>
                    {
                        currentSentence = sentence;
                        FadeOutNotesAfterLastSentence();
                    }));
            }
        }
        else
        {
            // Immediately remove all notes to have space for the next sentence
            currentSentence = sentence;
            RemoveAllDisplayedNotes();
        }

        if (sentence == null
            || !medleyControl.IsSentenceInMedleyRange(sentence))
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

    private void FadeOutNotesAfterLastSentence()
    {
        LeanTween.value(gameObject, targetNoteEntryContainer.resolvedStyle.opacity, 0, 1f)
            .setOnUpdate(interpolatedValue =>
            {
                targetNoteEntryContainer.style.opacity = interpolatedValue;
                recordedNoteEntryContainer.style.opacity = interpolatedValue;
            })
            .setOnComplete(() => RemoveAllDisplayedNotes());
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

    protected override void UpdateTargetNoteControl(TargetNoteControl targetNoteControl, int indexInList)
    {
        UpdateNotePosition(targetNoteControl.VisualElement, targetNoteControl.Note.MidiNote, targetNoteControl.Note.StartBeat, targetNoteControl.Note.EndBeat);
        UpdateTargetNoteLabelWith(targetNoteControl, indexInList);
    }

    public override float GetXInPercent(double positionInMillis)
    {
        if (currentSentence == null)
        {
            return 0;
        }

        int sentenceStartBeat = currentSentence.MinBeat;
        int sentenceLengthInBeat = currentSentence.LengthInBeats;
        double sentenceStartInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, sentenceStartBeat);
        double sentenceLengthInMillis = SongMetaBpmUtils.MillisPerBeat(songMeta) * sentenceLengthInBeat;
        double delayInMillis = micProfile?.DelayInMillis ?? 0;
        double xPercent = (float)(positionInMillis - sentenceStartInMillis - delayInMillis) / sentenceLengthInMillis;
        return (float)xPercent;
    }

    protected override bool TryGetNotePositionInPercent(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat, out Rect result)
    {
        if (currentSentence == null)
        {
            result = Rect.zero;
            return false;
        }

        int sentenceStartBeat = currentSentence.MinBeat;
        int sentenceEndBeat = currentSentence.MaxBeat;
        int beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        Vector2 yStartEndPercent = GetYStartAndEndInPercentForMidiNote(midiNote, (int)noteStartBeat);
        float yStartPercent = yStartEndPercent.x;
        float yEndPercent = yStartEndPercent.y;
        float xStartPercent = (float)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        float xEndPercent = (float)(noteEndBeat - sentenceStartBeat) / beatsInSentence;

        yStartPercent *= 100;
        yEndPercent *= 100;
        xStartPercent *= 100;
        xEndPercent *= 100;
        result = new Rect(xStartPercent, yStartPercent, xEndPercent - xStartPercent, yEndPercent - yStartPercent);
        return true;
    }
}
