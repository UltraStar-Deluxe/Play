using System;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Takes the analyzed pitches from the PlayerPitchTracker and creates display events to draw recorded notes.
// For example, multiple beats next to each other can be considered as one note.
[RequireComponent(typeof(PlayerMicPitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerMicPitchTracker PlayerMicPitchTracker { get; private set; }

    private RecordedNote lastRecordedNote;
    private PlayerMicPitchTracker.BeatAnalyzedEvent lastBeatAnalyzedEvent;

    private readonly Subject<RecordedNoteStartedEvent> recordedNoteStartedEventStream = new();
    public IObservable<RecordedNoteStartedEvent> RecordedNoteStartedEventStream
    {
        get
        {
            return recordedNoteStartedEventStream;
        }
    }

    private readonly Subject<RecordedNoteContinuedEvent> recordedNoteContinuedEventStream = new();
    public IObservable<RecordedNoteContinuedEvent> RecordedNoteContinuedEventStream
    {
        get
        {
            return recordedNoteContinuedEventStream;
        }
    }


    public void OnInjectionFinished()
    {
        PlayerMicPitchTracker.BeatAnalyzedEventStream
            .Subscribe(OnBeatAnalyzed);
    }

    private void OnBeatAnalyzed(PlayerMicPitchTracker.BeatAnalyzedEvent beatAnalyzedEvent)
    {
        Note analyzedNote = beatAnalyzedEvent.NoteAtBeat;
        if (lastRecordedNote != null
            && lastBeatAnalyzedEvent != null
            && lastBeatAnalyzedEvent.NoteAtBeat == analyzedNote
            && lastBeatAnalyzedEvent.RoundedRecordedMidiNote == beatAnalyzedEvent.RoundedRecordedMidiNote)
        {
            int noteEndBeat = analyzedNote != null
                ? analyzedNote.EndBeat
                : -1;
            ContinueLastRecordedNote(beatAnalyzedEvent.Beat, noteEndBeat);
        }
        else if (beatAnalyzedEvent.PitchEvent != null)
        {
            StartNewRecordedNote(beatAnalyzedEvent.Beat,
                beatAnalyzedEvent.NoteAtBeat,
                beatAnalyzedEvent.SentenceAtBeat,
                beatAnalyzedEvent.PitchEvent.MidiNote,
                beatAnalyzedEvent.RoundedRecordedMidiNote);
        }
        else
        {
            lastRecordedNote = null;
        }

        lastBeatAnalyzedEvent = beatAnalyzedEvent;
    }

    private void ContinueLastRecordedNote(int analyzedBeat, int targetNoteEndBeat)
    {
        lastRecordedNote.EndBeat = analyzedBeat + 1;
        if (targetNoteEndBeat >= 0
            && lastRecordedNote.EndBeat > targetNoteEndBeat)
        {
            lastRecordedNote.EndBeat = targetNoteEndBeat;
        }
        recordedNoteContinuedEventStream.OnNext(new RecordedNoteContinuedEvent(lastRecordedNote));
    }

    private void StartNewRecordedNote(int analyzedBeat, Note noteAtBeat, Sentence sentenceAtBeat, int recordedMidiNote, int roundedMidiNote)
    {
        RecordedNote newRecordedNote = new(recordedMidiNote, roundedMidiNote, analyzedBeat, analyzedBeat + 1, noteAtBeat, sentenceAtBeat);
        recordedNoteStartedEventStream.OnNext(new RecordedNoteStartedEvent(newRecordedNote));
        lastRecordedNote = newRecordedNote;
    }

    public class RecordedNoteStartedEvent
    {
        public RecordedNote RecordedNote { get; private set; }

        public RecordedNoteStartedEvent(RecordedNote recordedNote)
        {
            this.RecordedNote = recordedNote;
        }
    }

    public class RecordedNoteContinuedEvent
    {
        public RecordedNote RecordedNote { get; private set; }

        public RecordedNoteContinuedEvent(RecordedNote recordedNote)
        {
            this.RecordedNote = recordedNote;
        }
    }
}
