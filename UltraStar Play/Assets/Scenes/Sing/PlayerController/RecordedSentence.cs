
using System;
using System.Collections.Generic;

[Serializable]
public class RecordedSentence
{
    public Sentence Sentence { get; private set; }
    public List<RecordedNote> RecordedNotes { get; private set; }

    public RecordedSentence(Sentence sentence)
    {
        this.Sentence = sentence;
        this.RecordedNotes = new List<RecordedNote>();
    }

    public void AddRecordedNote(RecordedNote recordedNote)
    {
        RecordedNotes.Add(recordedNote);
    }
}