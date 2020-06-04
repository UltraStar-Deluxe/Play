using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerScoreControllerData
{
    public Dictionary<Sentence, SentenceScore> SentenceToSentenceScoreMap { get; set; } = new Dictionary<Sentence, SentenceScore>();
    public Dictionary<Note, NoteScore> NoteToNoteScoreMap { get; set; } = new Dictionary<Note, NoteScore>();

    public PlayerScoreControllerBeatData NormalBeatData { get; private set; } = new PlayerScoreControllerBeatData(EPlayerScoreControllerBeatDataNoteType.Normal);
    public PlayerScoreControllerBeatData GoldenBeatData { get; private set; } = new PlayerScoreControllerBeatData(EPlayerScoreControllerBeatDataNoteType.Golden);

    public int NormalNoteLengthTotal { get; set; }
    public int GoldenNoteLengthTotal { get; set; }

    public int PerfectSentenceCount { get; set; }
    public int TotalSentenceCount { get; set; }

    public int TotalScore { get; set; }
    public int NormalNotesTotalScore { get; set; }
    public int GoldenNotesTotalScore { get; set; }
    public int PerfectSentenceBonusTotalScore { get; set; }

    public PlayerScoreControllerBeatData GetBeatData(Note note)
    {
        if (note.IsNormal)
        {
            return NormalBeatData;
        }
        else if (note.IsGolden)
        {
            return GoldenBeatData;
        }
        return null;
    }
}

[Serializable]
public class SentenceScore
{
    public Sentence Sentence { get; private set; }

    public PlayerScoreControllerBeatData NormalBeatData { get; private set; } = new PlayerScoreControllerBeatData(EPlayerScoreControllerBeatDataNoteType.Normal);
    public PlayerScoreControllerBeatData GoldenBeatData { get; private set; } = new PlayerScoreControllerBeatData(EPlayerScoreControllerBeatDataNoteType.Golden);

    public int TotalScoreSoFar { get; set; }

    public SentenceScore(Sentence sentence)
    {
        Sentence = sentence;
    }

    public PlayerScoreControllerBeatData GetBeatData(Note note)
    {
        if (note.IsNormal)
        {
            return NormalBeatData;
        }
        else if (note.IsGolden)
        {
            return GoldenBeatData;
        }
        return null;
    }
}

[Serializable]
public class NoteScore
{
    public Note Note { get; private set; }
    public int CorrectlySungBeats { get; set; }

    public bool IsPerfect
    {
        get
        {
            return CorrectlySungBeats >= Note.Length;
        }
    }

    public NoteScore(Note note)
    {
        Note = note;
    }
}

[Serializable]
public class PlayerScoreControllerBeatData
{
    public EPlayerScoreControllerBeatDataNoteType NoteType { get; private set; }
    public int PerfectBeats { get; set; }
    public int GoodBeats { get; set; }

    public int PerfectAndGoodBeats => PerfectBeats + GoodBeats;

    public PlayerScoreControllerBeatData(EPlayerScoreControllerBeatDataNoteType noteType)
    {
        NoteType = noteType;
    }
}

public enum EPlayerScoreControllerBeatDataNoteType
{
    Normal, Golden
}
