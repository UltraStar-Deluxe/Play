using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using Unity.Collections.LowLevel.Unsafe;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Takes the analyzed beats of the PlayerPitchTracker and calculates the player's score.
public class PlayerScoreController : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // A total of 10000 points can be achieved.
    // Golden notes give double points.
    // Singing perfect lines gives a bonus of up to 1000 points.
    public static readonly int MaxScore = 10000;
    public static readonly int MaxPerfectSentenceBonusScore = 1000;
    public static readonly int MaxScoreForNotes = MaxScore - MaxPerfectSentenceBonusScore;

    public int TotalScore
    {
        get
        {
            return NormalNotesTotalScore + GoldenNotesTotalScore + PerfectSentenceBonusTotalScore;
        }
    }

    public int NormalNotesTotalScore
    {
        get
        {
            if (normalNoteLengthTotal <= 0)
            {
                return 0;
            }
            return (int)(maxScoreForNormalNotes * correctNormalNoteLengthTotal / normalNoteLengthTotal);
        }
    }

    public int GoldenNotesTotalScore
    {
        get
        {
            if (goldenNoteLengthTotal <= 0)
            {
                return 0;
            }
            return (int)(maxScoreForGoldenNotes * correctGoldenNoteLengthTotal / goldenNoteLengthTotal);
        }
    }

    public int PerfectSentenceBonusTotalScore
    {
        get
        {
            int targetSentenceCount = (sentenceCount > 20) ? 20 : sentenceCount;
            double score = (double)MaxPerfectSentenceBonusScore * perfectSentenceCount / targetSentenceCount;

            // Round the score up
            score = Math.Ceiling(score);
            if (score > MaxPerfectSentenceBonusScore)
            {
                score = MaxPerfectSentenceBonusScore;
            }
            return (int)score;
        }
    }

    [Inject]
    private PlayerPitchTracker playerPitchTracker;

    [Inject]
    private Voice voice;

    private Dictionary<Sentence, SentenceScore> sentenceToSentenceScoreMap = new Dictionary<Sentence, SentenceScore>();
    private Dictionary<Note, NoteScore> noteToNoteScoreMap = new Dictionary<Note, NoteScore>();

    private Subject<SentenceScoreEvent> sentenceScoreEventStream = new Subject<SentenceScoreEvent>();
    public IObservable<SentenceScoreEvent> SentenceScoreEventStream
    {
        get
        {
            return sentenceScoreEventStream;
        }
    }

    private Subject<NoteScoreEvent> noteScoreEventStream = new Subject<NoteScoreEvent>();
    public IObservable<NoteScoreEvent> NoteScoreEventStream
    {
        get
        {
            return noteScoreEventStream;
        }
    }

    private int perfectSentenceCount;
    private int sentenceCount;

    private double maxScoreForNormalNotes;
    private double maxScoreForGoldenNotes;

    private double normalNoteLengthTotal;
    private double goldenNoteLengthTotal;

    private double correctNormalNoteLengthTotal;
    private double correctGoldenNoteLengthTotal;

    public void OnInjectionFinished()
    {
        UpdateMaxScores(voice.Sentences);

        playerPitchTracker.BeatAnalyzedEventStream.Subscribe(OnBeatAnalyzed);
        playerPitchTracker.NoteAnalyzedEventStream.Subscribe(OnNoteAnalyzed);
        playerPitchTracker.SentenceAnalyzedEventStream.Subscribe(OnSentenceAnalyzed);
    }

    private void OnBeatAnalyzed(PlayerPitchTracker.BeatAnalyzedEvent beatAnalyzedEvent)
    {
        // Check if pitch was detected where a note is expected in the song
        if (beatAnalyzedEvent.PitchEvent == null
            || beatAnalyzedEvent.NoteAtBeat == null)
        {
            return;
        }

        Note analyzedNote = beatAnalyzedEvent.NoteAtBeat;

        // Check if note was hit
        if (MidiUtils.GetRelativePitch(beatAnalyzedEvent.RoundedMidiNote) != MidiUtils.GetRelativePitch(analyzedNote.MidiNote))
        {
            return;
        }

        // The beat was sung correctly.
        if (!noteToNoteScoreMap.TryGetValue(analyzedNote, out NoteScore noteScore))
        {
            noteScore = new NoteScore(analyzedNote);
            noteToNoteScoreMap.Add(analyzedNote, noteScore);
        }
        noteScore.correctlySungBeats++;

        Sentence analyzedSentence = beatAnalyzedEvent.NoteAtBeat.Sentence;
        if (!sentenceToSentenceScoreMap.TryGetValue(analyzedSentence, out SentenceScore sentenceScore))
        {
            sentenceScore = new SentenceScore(analyzedSentence);
            sentenceToSentenceScoreMap.Add(analyzedSentence, sentenceScore);
        }

        if (analyzedNote.IsNormal)
        {
            correctNormalNoteLengthTotal++;
            sentenceScore.CorrectlySungNormalBeats++;
        }
        else if (analyzedNote.IsGolden)
        {
            correctGoldenNoteLengthTotal++;
            sentenceScore.CorrectlySungGoldenBeats++;
        }
    }

    private void OnNoteAnalyzed(PlayerPitchTracker.NoteAnalyzedEvent noteAnalyzedEvent)
    {
        Note analyzedNote = noteAnalyzedEvent.Note;
        if (noteToNoteScoreMap.TryGetValue(analyzedNote, out NoteScore noteScore))
        {
            //Debug.Log($"OnNoteAnalyzed: {noteScore.correctlySungBeats} / {analyzedNote.Length}, {analyzedNote.StartBeat}, {analyzedNote.EndBeat}, {analyzedNote.Text}");
            if (noteScore.correctlySungBeats >= analyzedNote.Length)
            {
                noteScoreEventStream.OnNext(new NoteScoreEvent(noteScore));
            }
        }
    }

    private void OnSentenceAnalyzed(PlayerPitchTracker.SentenceAnalyzedEvent sentenceAnalyzedEvent)
    {
        Sentence analyzedSentence = sentenceAnalyzedEvent.Sentence;
        int totalScorableNoteLength = analyzedSentence.Notes
                .Where(note => note.IsNormal || note.IsGolden)
                .Select(note => note.Length)
                .Sum();

        if (totalScorableNoteLength <= 0)
        {
            return;
        }

        SentenceRating sentenceRating;
        if (sentenceToSentenceScoreMap.TryGetValue(analyzedSentence, out SentenceScore sentenceScore))
        {
            int correctlySungNoteLength = sentenceScore.CorrectlySungNormalBeats + sentenceScore.CorrectlySungGoldenBeats;
            double correctNotesPercentage = (double)correctlySungNoteLength / totalScorableNoteLength;

            // Score for a perfect sentence
            if (correctNotesPercentage >= SentenceRating.Perfect.PercentageThreshold)
            {
                perfectSentenceCount++;
            }

            sentenceRating = GetSentenceRating(correctNotesPercentage);
        }
        else
        {
            sentenceScore = new SentenceScore(analyzedSentence);
            sentenceRating = GetSentenceRating(0);
        }
        sentenceScoreEventStream.OnNext(new SentenceScoreEvent(sentenceScore, sentenceRating));
    }

    private void UpdateMaxScores(IReadOnlyCollection<Sentence> sentences)
    {
        // Calculate the points for a single beat of a normal or golden note
        normalNoteLengthTotal = 0;
        goldenNoteLengthTotal = 0;
        foreach (Sentence sentence in sentences)
        {
            normalNoteLengthTotal += GetNormalNoteLength(sentence);
            goldenNoteLengthTotal += GetGoldenNoteLength(sentence);
        }

        double scoreForCorrectBeatOfNormalNotes = MaxScoreForNotes / (normalNoteLengthTotal + (2 * goldenNoteLengthTotal));
        double scoreForCorrectBeatOfGoldenNotes = 2 * scoreForCorrectBeatOfNormalNotes;

        maxScoreForNormalNotes = scoreForCorrectBeatOfNormalNotes * normalNoteLengthTotal;
        maxScoreForGoldenNotes = scoreForCorrectBeatOfGoldenNotes * goldenNoteLengthTotal;

        // Countercheck: The sum of all points must be equal to MaxScoreForNotes
        double pointsForAllNotes = maxScoreForNormalNotes + maxScoreForGoldenNotes;
        bool isSound = Math.Abs(MaxScoreForNotes - pointsForAllNotes) <= 0.01;
        if (!isSound)
        {
            Debug.LogWarning("The definition of scores for normal or golden notes is not sound.");
        }

        // Round the values for the max score of normal / golden notes to avoid floating point inaccuracy.
        maxScoreForNormalNotes = Math.Ceiling(maxScoreForNormalNotes);
        maxScoreForGoldenNotes = Math.Ceiling(maxScoreForGoldenNotes);
        // The sum of the rounded points must not exceed the MaxScoreForNotes.
        // If the definition is sound then the overhang is at most 2 because of the above rounding.
        int overhang = (int)(maxScoreForNormalNotes + maxScoreForGoldenNotes) - MaxScoreForNotes;
        maxScoreForNormalNotes -= overhang;

        // Remember the sentence count to calculate the points for a perfect sentence.
        sentenceCount = sentences.Count;
    }

    private int GetNormalNoteLength(Sentence sentence)
    {
        return sentence.Notes.Where(note => note.IsNormal).Select(note => (int)note.Length).Sum();
    }

    private int GetGoldenNoteLength(Sentence sentence)
    {
        return sentence.Notes.Where(note => note.IsGolden).Select(note => (int)note.Length).Sum();
    }

    private SentenceRating GetSentenceRating(double correctNotesPercentage)
    {
        if (correctNotesPercentage < 0)
        {
            return null;
        }

        foreach (SentenceRating sentenceRating in SentenceRating.Values)
        {
            if (correctNotesPercentage >= sentenceRating.PercentageThreshold)
            {
                return sentenceRating;
            }
        }
        return null;
    }

    public class SentenceScoreEvent
    {
        public SentenceScore SentenceScore { get; private set; }
        public SentenceRating SentenceRating { get; private set; }

        public SentenceScoreEvent(SentenceScore sentenceScore, SentenceRating sentenceRating)
        {
            SentenceScore = sentenceScore;
            SentenceRating = sentenceRating;
        }
    }

    public class NoteScoreEvent
    {
        public NoteScore NoteScore { get; private set; }

        public NoteScoreEvent(NoteScore noteScore)
        {
            NoteScore = noteScore;
        }
    }

    public class SentenceScore
    {
        public Sentence Sentence { get; private set; }
        public int CorrectlySungNormalBeats { get; set; }
        public int CorrectlySungGoldenBeats { get; set; }

        public SentenceScore(Sentence sentence)
        {
            Sentence = sentence;
        }
    }

    public class NoteScore
    {
        public Note Note { get; private set; }
        public int correctlySungBeats { get; set; }

        public bool IsPerfect
        {
            get
            {
                return correctlySungBeats >= Note.Length;
            }
        }

        public NoteScore(Note note)
        {
            Note = note;
        }
    }
}
