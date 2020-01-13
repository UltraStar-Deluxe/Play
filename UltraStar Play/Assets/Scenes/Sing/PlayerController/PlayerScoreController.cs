using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerScoreController : MonoBehaviour
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

    private int perfectSentenceCount;
    private int sentenceCount;

    private double maxScoreForNormalNotes;
    private double maxScoreForGoldenNotes;

    private double normalNoteLengthTotal;
    private double goldenNoteLengthTotal;

    private double correctNormalNoteLengthTotal;
    private double correctGoldenNoteLengthTotal;

    public void Init(Voice voice)
    {
        UpdateMaxScores(voice.Sentences);
    }

    public SentenceRating CalculateScoreForSentence(Sentence sentence, List<RecordedNote> recordedNotes)
    {
        if (sentence.Notes == null || sentence.Notes.Count == 0)
        {
            return null;
        }

        if (recordedNotes == null || recordedNotes.Count == 0)
        {
            return SentenceRating.Awful;
        }

        // Correctly sung notes
        double correctNormalNoteLength = recordedNotes.Select(it => GetCorrectlySungNormalNoteLength(it)).Sum();
        double correctGoldenNoteLength = recordedNotes.Select(it => GetCorrectlySungGoldenNoteLength(it)).Sum();
        double correctNotesLength = correctNormalNoteLength + correctGoldenNoteLength;
        double totalNotesLength = GetNormalNoteLength(sentence) + GetGoldenNoteLength(sentence);
        double correctNotesPercentage = correctNotesLength / totalNotesLength;

        // Sum up correctly sung beats
        correctNormalNoteLengthTotal += (int)correctNormalNoteLength;
        correctGoldenNoteLengthTotal += (int)correctGoldenNoteLength;

        // Score for a perfect sentence
        if (correctNotesPercentage >= SentenceRating.Perfect.PercentageThreshold)
        {
            perfectSentenceCount++;
        }

        SentenceRating sentenceRating = GetSentenceRating(sentence, correctNotesPercentage);
        return sentenceRating;
    }

    private int GetCorrectlySungNormalNoteLength(RecordedNote recordedNote)
    {
        if (recordedNote.TargetNote == null || !recordedNote.TargetNote.IsNormal)
        {
            return 0;
        }

        return GetCorrectlySungNoteLength(recordedNote);
    }

    private int GetCorrectlySungGoldenNoteLength(RecordedNote recordedNote)
    {
        if (recordedNote.TargetNote == null || !recordedNote.TargetNote.IsGolden)
        {
            return 0;
        }

        return GetCorrectlySungNoteLength(recordedNote);
    }

    private int GetCorrectlySungNoteLength(RecordedNote recordedNote)
    {
        if (recordedNote.TargetNote == null)
        {
            return 0;
        }

        if (MidiUtils.GetRelativePitch(recordedNote.TargetNote.MidiNote) != MidiUtils.GetRelativePitch(recordedNote.RoundedMidiNote))
        {
            return 0;
        }

        int correctlySungNoteLength = (int)(recordedNote.EndBeat - recordedNote.StartBeat);
        return correctlySungNoteLength;
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
        bool isSound = (MaxScoreForNotes == pointsForAllNotes);
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

    public SentenceRating GetSentenceRating(Sentence currentSentence, double correctNotesPercentage)
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
}
