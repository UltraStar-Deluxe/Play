using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class MutableSentenceTests
{
    private Sentence ms;

    [SetUp]
    public void TestInit()
    {
        ms = new Sentence();
    }

    [Test]
    public void TestCreation()
    {
        Assert.AreEqual(0, ms.Notes.Count);
        Assert.AreEqual(0, ms.LinebreakBeat);
    }

    [Test]
    public void TestNullNoteThrowsException()
    {
        Assert.Throws<ArgumentNullException>(delegate { ms.AddNote(null); });
    }

    [Test]
    public void TestLinebreakBeat()
    {
        ms.SetLinebreakBeat(2);
        Assert.AreEqual(2, ms.LinebreakBeat);
    }

    [Test]
    public void TestNotes()
    {
        Note testNote = new Note(ENoteType.Normal, 0, 2, 0, "");
        ms.AddNote(testNote);
        IReadOnlyCollection<Note> notes = ms.Notes;
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(testNote, notes.FirstOrDefault());
    }
}