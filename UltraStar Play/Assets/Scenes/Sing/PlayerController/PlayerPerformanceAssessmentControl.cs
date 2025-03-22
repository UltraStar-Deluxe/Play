using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class PlayerPerformanceAssessmentControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private PlayerMicPitchTracker playerMicPitchTracker;

    [Inject]
    private SingSceneMedleyControl medleyControl;

    [Inject]
    private Voice voice;

    private readonly Subject<NoteAssessedEvent> noteAssessedEventStream = new();
    public IObservable<NoteAssessedEvent> NoteAssessedEventStream => noteAssessedEventStream;

    private readonly Subject<SentenceAssessedEvent> sentenceAssessedEventStream = new();
    public IObservable<SentenceAssessedEvent> SentenceAssessedEventStream => sentenceAssessedEventStream;

    private readonly Dictionary<Note, HashSet<int>> noteToCorrectlySungBeats = new();
    private readonly HashSet<int> processedBeats = new();

    private int firstBeatToScoreInclusive;
    private int lastBeatToScoreExclusive;

    public void OnInjectionFinished()
    {
        PrepareBeatToBeAnalyzedChecks();

        playerMicPitchTracker.BeatAnalyzedEventStream.Subscribe(OnBeatAnalyzed);
        playerMicPitchTracker.NoteAnalyzedEventStream.Subscribe(OnNoteAnalyzed);
        playerMicPitchTracker.SentenceAnalyzedEventStream.Subscribe(OnSentenceAnalyzed);
    }

    private void PrepareBeatToBeAnalyzedChecks()
    {
        List<Note> notes = voice
            .Sentences
            .SelectMany(s => s.Notes)
            .ToList();
        firstBeatToScoreInclusive = SongMetaUtils.GetMinBeat(notes);
        lastBeatToScoreExclusive = SongMetaUtils.GetMaxBeat(notes);
    }

    private void OnBeatAnalyzed(BeatAnalyzedEvent beatAnalyzedEvent)
    {
        int beat = beatAnalyzedEvent.Beat;
        if (beat < firstBeatToScoreInclusive
            || beat >= lastBeatToScoreExclusive
            || !medleyControl.IsBeatInMedleyRange(beat))
        {
            return;
        }

        if (processedBeats.Contains(beat))
        {
            Debug.LogWarning($"Attempt to assess beat multiple times: {beat}");
            return;
        }
        processedBeats.Add(beat);

        if (beatAnalyzedEvent.PitchEvent == null
            || beatAnalyzedEvent.NoteAtBeat == null)
        {
            return;
        }

        Note noteAtBeat = beatAnalyzedEvent.NoteAtBeat;
        if (noteAtBeat == null
            || !SongMetaUtils.IsBeatInNote(noteAtBeat, beat, true, false))
        {
            return;
        }

        if (!IsCorrectlySung(beatAnalyzedEvent))
        {
            return;
        }

        AddCorrectlySungBeat(beat, noteAtBeat);
    }

    private void OnNoteAnalyzed(NoteAnalyzedEvent noteAnalyzedEvent)
    {
        Note note = noteAnalyzedEvent.Note;
        if (!medleyControl.IsNoteInMedleyRange(note))
        {
            return;
        }

        noteAssessedEventStream.OnNext(new NoteAssessedEvent(note, GetCorrectlySungBeats(note)));
    }

    private void OnSentenceAnalyzed(SentenceAnalyzedEvent sentenceAnalyzedEvent)
    {
        Sentence sentence = sentenceAnalyzedEvent.Sentence;
        if (!medleyControl.IsSentenceInMedleyRange(sentence))
        {
            return;
        }

        IReadOnlyCollection<int> correctlySungBeats = sentence.Notes
            .SelectMany(note => GetCorrectlySungBeats(note))
            .ToHashSet();
        sentenceAssessedEventStream.OnNext(new SentenceAssessedEvent(sentence, correctlySungBeats));
    }

    private void AddCorrectlySungBeat(int beat, Note noteAtBeat)
    {
        if (!noteToCorrectlySungBeats.TryGetValue(noteAtBeat, out HashSet<int> correctlySungBeats))
        {
            correctlySungBeats = new HashSet<int>();
            noteToCorrectlySungBeats[noteAtBeat] = correctlySungBeats;
        }
        correctlySungBeats.Add(beat);
    }

    private bool IsCorrectlySung(BeatAnalyzedEvent beatAnalyzedEvent)
    {
        return beatAnalyzedEvent.NoteAtBeat != null
               && beatAnalyzedEvent.NoteAtBeat.MidiNote == beatAnalyzedEvent.RoundedRecordedMidiNote;
    }

    private IReadOnlyCollection<int> GetCorrectlySungBeats(Note note)
    {
        if (!noteToCorrectlySungBeats.TryGetValue(note, out HashSet<int> correctlySungBeats))
        {
            return new HashSet<int>();
        }

        return correctlySungBeats;
    }

    public class NoteAssessedEvent
    {
        public Note Note { get; }
        public IReadOnlyCollection<int> CorrectlySungBeats { get; }

        private readonly double correctlySungBeatsPercent;
        public double CorrectlySungBeatsPercent => correctlySungBeatsPercent;

        public bool IsPerfect => CorrectlySungBeatsPercent >= 1;

        public NoteAssessedEvent(Note note, IReadOnlyCollection<int> correctlySungBeats)
        {
            if (!correctlySungBeats.AllMatch(beat =>
                    SongMetaUtils.IsBeatInNote(note, beat, true, false)))
            {
                throw new ArgumentException("correctly sung beats must be inside note");
            }

            Note = note;
            CorrectlySungBeats = correctlySungBeats;

            correctlySungBeatsPercent = note.Length <= 0
                ? 0
                : correctlySungBeatsPercent = (double)CorrectlySungBeats.Count / note.Length;
        }
    }

    public class SentenceAssessedEvent
    {
        public Sentence Sentence { get; }
        public IReadOnlyCollection<int> CorrectlySungBeats { get; }

        private readonly double correctlySungBeatsPercent;
        public double CorrectlySungBeatsPercent => correctlySungBeatsPercent;

        public bool IsPerfect => CorrectlySungBeatsPercent >= SentenceRating.perfect.PercentageThreshold;
        public SentenceRating SentenceRating => SentenceRating.GetSentenceRating(CorrectlySungBeatsPercent);

        public SentenceAssessedEvent(Sentence sentence, IReadOnlyCollection<int> correctlySungBeats)
        {
            if (!correctlySungBeats.AllMatch(beat =>
                    sentence.Notes.AnyMatch(note =>
                        SongMetaUtils.IsBeatInNote(note, beat, true, false))))
            {
                throw new ArgumentException("correctly sung beats must be inside note");
            }

            Sentence = sentence;
            CorrectlySungBeats = correctlySungBeats;

            int noteLengthSum = Sentence
                .Notes
                .Select(note => note.Length)
                .Sum();
            correctlySungBeatsPercent = noteLengthSum <= 0
                ? 0
                : correctlySungBeatsPercent = (double)CorrectlySungBeats.Count / noteLengthSum;
        }
    }
}
