using System;
using NUnit.Framework;
using System.Collections.Generic;

public class MutableSentenceTests
{
    private MutableSentence ms;

    [SetUp]
    public void TestInit()
    {
        ms = new MutableSentence();
    }

    [Test]
    public void TestCreation()
    {
        Assert.AreEqual(0, ms.GetNotes().Count);
        Assert.AreEqual(0, ms.GetLinebreakBeat());
    }

    [Test]
    public void TestNullNoteThrowsException()
    {
        Assert.Throws<ArgumentNullException>(delegate { ms.Add(null); });
    }

    [Test]
    public void TestLinebreakBeat()
    {
        ms.SetLinebreakBeat(2);
        Assert.AreEqual(2, ms.GetLinebreakBeat());
    }

    [Test]
    public void TestAddingNotesAfterLinebreakBeatThrowsException()
    {
        ms.SetLinebreakBeat(2);
        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { ms.Add(new Note(ENoteType.Normal, 0, 1, 0, "")); });
        Assert.AreEqual("Adding more notes after the linebreak has already been set is not allowed", vbe.Message);
    }

    [Test]
    public void TestOverlappingNoteThrowsException()
    {
        ms.Add(new Note(ENoteType.Normal, 0, 2, 0, ""));
        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { ms.Add(new Note(ENoteType.Normal, 1, 1, 0, "")); });
        Assert.AreEqual("New note overlaps with existing sentence", vbe.Message);
    }

    [Test]
    public void TestNotes()
    {
        Note testNote = new Note(ENoteType.Normal, 0, 2, 0, "");
        ms.Add(testNote);
        List<Note> notes = ms.GetNotes();
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(testNote, notes[0]);
    }

    [Test]
    public void TestOverlappingLinebreakThrowsException()
    {
        ms.Add(new Note(ENoteType.Normal, 0, 2, 0, ""));
        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { ms.SetLinebreakBeat(1); });
        Assert.AreEqual("Linebreak conflicts with existing sentence", vbe.Message);
    }
}