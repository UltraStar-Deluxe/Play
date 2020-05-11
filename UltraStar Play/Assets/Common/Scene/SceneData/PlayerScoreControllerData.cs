using System;
using System.Collections.Generic;

[Serializable]
public class PlayerScoreControllerData
{
    public Dictionary<Sentence, SentenceScore> SentenceToSentenceScoreMap { get; set; } = new Dictionary<Sentence, SentenceScore>();
    public Dictionary<Note, NoteScore> NoteToNoteScoreMap { get; set; } = new Dictionary<Note, NoteScore>();

    public double NormalNoteLengthTotal { get; set; }
    public double GoldenNoteLengthTotal { get; set; }

    public double CorrectNormalNoteLengthTotal { get; set; }
    public double CorrectGoldenNoteLengthTotal { get; set; }
}

[Serializable]
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

[Serializable]
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
