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
    public const int MaxScore = 10000;
    public const int MaxPerfectSentenceBonusScore = 1000;
    public const int MaxScoreForNotes = MaxScore - MaxPerfectSentenceBonusScore;

    public double TotalScore
    {
        get
        {
            return NormalNotesTotalScore + GoldenNotesTotalScore + PerfectSentenceBonusTotalScore;
        }
    }
    public double NormalNotesTotalScore { get; private set; }
    public double GoldenNotesTotalScore { get; private set; }
    public double PerfectSentenceBonusTotalScore { get; private set; }

    private double ScoreForACorrectBeatOfNormalNotes { get; set; }
    private double ScoreForACorrectBeatOfGoldenNotes { get; set; }
    private double ScoreForAPerfectSentence { get; set; }

    public void Init(Voice voice)
    {
        UpdateMaxScores(voice.Sentences);
    }

    public double CalculateScoreForSentence(Sentence sentence, List<RecordedNote> recordedNotes)
    {
        if (sentence.Notes == null || sentence.Notes.Count == 0)
        {
            return -1;
        }

        if (recordedNotes == null || recordedNotes.Count == 0)
        {
            return 0;
        }

        // Correctly sung notes
        double correctNormalNoteLength = GetCorrectlySungNoteLength(sentence.NormalNotes, recordedNotes);
        double correctGoldenNoteLength = GetCorrectlySungNoteLength(sentence.GoldenNotes, recordedNotes);
        double correctNotesLength = correctNormalNoteLength + correctGoldenNoteLength;
        double totalNotesLength = GetNormalNoteLength(sentence) + GetGoldenNoteLength(sentence);
        double correctNotesPercentage = correctNotesLength / totalNotesLength;

        // Score for notes
        double scoreForNormalNotes = correctNormalNoteLength * ScoreForACorrectBeatOfNormalNotes;
        double scoreForGoldenNotes = correctGoldenNoteLength * ScoreForACorrectBeatOfGoldenNotes;
        NormalNotesTotalScore += scoreForNormalNotes;
        GoldenNotesTotalScore += scoreForGoldenNotes;

        // Score for a perfect sentence
        if (correctNotesPercentage >= SentenceRating.Perfect.PercentageThreshold)
        {
            PerfectSentenceBonusTotalScore = (PerfectSentenceBonusTotalScore + ScoreForAPerfectSentence);
        }
        // Not all sentences need to be perfect to achieve the maximum perfect sentence bonus score.
        // Thus, the limit has to be checked that it does not exceed the maximum.
        if (PerfectSentenceBonusTotalScore > MaxPerfectSentenceBonusScore)
        {
            PerfectSentenceBonusTotalScore = MaxPerfectSentenceBonusScore;
        }

        return correctNotesPercentage;
    }

    private double GetCorrectlySungNoteLength(List<Note> notes, List<RecordedNote> recordedNotes)
    {
        double correctlySungOverlap = 0;
        foreach (Note note in notes)
        {
            foreach (RecordedNote recordedNote in recordedNotes)
            {
                double correctlySungOverlapOfNote = GetCorrectlySungOverlap(note, recordedNote);
                correctlySungOverlap += correctlySungOverlapOfNote;
            }
        }
        return correctlySungOverlap;
    }

    private double GetCorrectlySungOverlap(Note note, RecordedNote recordedNote)
    {
        if (note.MidiNote != recordedNote.RoundedMidiNote)
        {
            return 0;
        }

        return GetOverlap(note, recordedNote);
    }

    private double GetOverlap(Note note, RecordedNote recordedNote)
    {
        // No width that could overlap
        if (recordedNote.StartBeat == recordedNote.EndBeat || note.StartBeat == note.EndBeat)
        {
            return 0;
        }

        // note: |----|               |----|
        //  rec:         |--|   |--|
        if (recordedNote.StartBeat >= note.EndBeat || recordedNote.EndBeat <= note.StartBeat)
        {
            return 0;
        }

        // From here on, there must be some overlap, either inside or half outside.
        // note: |----|
        //  rec:  |--| 
        if (recordedNote.StartBeat >= note.StartBeat && recordedNote.EndBeat <= note.EndBeat)
        {
            // RecordedNote completely overlaps
            return recordedNote.LengthInBeats;
        }

        // note:    |----|
        //  rec:  |--| 
        if (recordedNote.StartBeat <= note.StartBeat)
        {
            return recordedNote.EndBeat - note.StartBeat;
        }

        // note:    |----|
        //  rec:       |--| 
        if (recordedNote.EndBeat >= note.EndBeat)
        {
            return note.EndBeat - recordedNote.StartBeat;
        }

        // This should never be reached
        Debug.LogError("Should never get here. " +
                        "GetOverlap must have a missing case in its definition " +
                        $"({note.StartBeat}, {note.EndBeat}) ({recordedNote.StartBeat}, {recordedNote.EndBeat}).");
        return 0;
    }

    private void UpdateMaxScores(List<Sentence> sentences)
    {
        // Calculate the points for a single beat of a normal or golden note
        double normalNoteLengthTotal = 0;
        double goldenNoteLengthTotal = 0;
        foreach (Sentence sentence in sentences)
        {
            normalNoteLengthTotal += GetNormalNoteLength(sentence);
            goldenNoteLengthTotal += GetGoldenNoteLength(sentence);
        }

        ScoreForACorrectBeatOfNormalNotes = MaxScoreForNotes / (normalNoteLengthTotal + (2 * goldenNoteLengthTotal));
        ScoreForACorrectBeatOfGoldenNotes = 2 * ScoreForACorrectBeatOfNormalNotes;

        // Countercheck: The sum of all points must be equal to MaxScoreForNotes
        double pointsForAllNotes = ScoreForACorrectBeatOfNormalNotes * normalNoteLengthTotal
                                 + ScoreForACorrectBeatOfGoldenNotes * goldenNoteLengthTotal;
        bool isSound = (MaxScoreForNotes == pointsForAllNotes);
        if (!isSound)
        {
            Debug.LogError("The definition of scores for normal or golden notes does not make sense.");
        }

        // Calculate score for a perfect line.
        // This is a bonus score of which the maximum amount can be achieved without all sentences beeing perfect.
        // Thus, there is a minimum value given here.
        // As a result, the score for perfect sentences has to be checked not to exceed the maximum.
        ScoreForAPerfectSentence = (int)Math.Ceiling((double)MaxPerfectSentenceBonusScore / sentences.Count);
        if (ScoreForAPerfectSentence < 50)
        {
            ScoreForAPerfectSentence = 50;
        }
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
