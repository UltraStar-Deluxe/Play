using NUnit.Framework;
using System.Collections.Generic;

public class SongDataStructureTests
{
    [Test]
    public void SentenceMinAndMaxBeatChangeWithNotes()
    {
        Note n0_2 = new Note(ENoteType.Normal, 0, 2, 0, "");
        Note n2_4 = new Note(ENoteType.Normal, 2, 2, 0, "");
        Note n4_6 = new Note(ENoteType.Normal, 4, 2, 0, "");

        Sentence s1 = new Sentence(new List<Note> { n2_4 }, 0);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);

        // Expand MinBeat and MaxBeat
        s1.AddNote(n4_6);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);

        s1.AddNote(n0_2);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);

        // Shrink MinBeat and MaxBeat
        s1.RemoveNote(n4_6);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);

        s1.RemoveNote(n0_2);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);
    }

    [Test]
    public void SentenceMinAndMaxBeatChangeWithNotePositions()
    {
        Note note = new Note(ENoteType.Normal, 2, 2, 0, "");

        Sentence s1 = new Sentence(new List<Note> { note }, 0);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);
        Assert.AreEqual(4, s1.LinebreakBeat);

        note.SetEndBeat(6);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);
        Assert.AreEqual(6, s1.LinebreakBeat);

        note.SetStartBeat(0);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);
        Assert.AreEqual(6, s1.LinebreakBeat);
    }
}