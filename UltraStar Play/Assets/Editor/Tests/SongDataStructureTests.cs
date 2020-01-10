using NUnit.Framework;
using System.Collections.Generic;

public class SongDataStructureTests
{
    [Test]
    public void SentenceMinAndMaxBeatIsSetWhenCreated()
    {
        Note n2_4 = new Note(ENoteType.Normal, 2, 2, 0, "");

        // MinBeat and MaxBeat are set when the sentence is directly created with notes.
        Sentence s1 = new Sentence(new List<Note> { n2_4 }, 0);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);

        // MinBeat and MaxBeat are set when the sentence is created empty and notes are added.
        Sentence s2 = new Sentence();
        s2.AddNote(n2_4);
        Assert.AreEqual(2, s2.MinBeat);
        Assert.AreEqual(4, s2.MaxBeat);
    }

    [Test]
    public void SentenceMinAndMaxBeatChangeWithNotes()
    {
        Note n0_2 = new Note(ENoteType.Normal, 0, 2, 0, "");
        Note n2_4 = new Note(ENoteType.Normal, 2, 2, 0, "");
        Note n4_6 = new Note(ENoteType.Normal, 4, 2, 0, "");

        Sentence s1 = new Sentence(new List<Note> { n2_4 }, 0);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);

        // Expand MinBeat and MaxBeat by adding notes
        s1.AddNote(n4_6);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);
        s1.AddNote(n0_2);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);

        // Shrink MinBeat and MaxBeat by removing notes
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
        Note n2_4 = new Note(ENoteType.Normal, 2, 2, 0, "");

        Sentence s1 = new Sentence(new List<Note> { n2_4 }, 0);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);

        // Expand MinBeat and MaxBeat by modifying notes
        n2_4.SetStartBeat(0);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);
        n2_4.SetLength(5);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(5, s1.MaxBeat);
        n2_4.SetEndBeat(6);
        Assert.AreEqual(0, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);

        // Shrink MinBeat and MaxBeat by modifying notes
        n2_4.SetStartBeat(2);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(6, s1.MaxBeat);
        n2_4.SetLength(3);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(5, s1.MaxBeat);
        n2_4.SetEndBeat(4);
        Assert.AreEqual(2, s1.MinBeat);
        Assert.AreEqual(4, s1.MaxBeat);
    }
}