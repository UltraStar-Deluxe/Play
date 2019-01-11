using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
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
        Assert.Throws<ArgumentNullException>(delegate { ms.Add(null); } );
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
        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { ms.Add(new Note(ENoteType.Normal, 0, 1, 0, "")); } );
        Assert.AreEqual("Adding more notes after the linebreak has already been set is not allowed", vbe.Message);
    }

    [Test]
    public void TestOverlappingNoteThrowsException()
    {
        ms.Add(new Note(ENoteType.Normal, 0, 2, 0, ""));
        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { ms.Add(new Note(ENoteType.Normal, 1, 1, 0, "")); } );
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
        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { ms.SetLinebreakBeat(1); } );
        Assert.AreEqual("Linebreak conflicts with existing sentence", vbe.Message);
    }
}

public class MutableVoiceTests
{
    private MutableVoice mv;

    [SetUp]
    public void TestInit()
    {
        mv = new MutableVoice();
    }

    [Test]
    public void TestCreation()
    {
        Assert.AreEqual(0, mv.GetSentences().Count);
    }

    [Test]
    public void TestNullSentenceThrowsException()
    {
        Assert.Throws<ArgumentNullException>(delegate { mv.Add(null); } );
    }

    [Test]
    public void TestOneSentence()
    {
        Note testNote = new Note(ENoteType.Normal, 0, 2, 0, "");
        MutableSentence ms = new MutableSentence();
        ms.Add(testNote);
        mv.Add((Sentence)ms);

        List<Sentence> sentences = mv.GetSentences();
        Assert.AreEqual(1, sentences.Count);
        List<Note> notes = sentences[0].Notes;
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(testNote, notes[0]);
    }

    [Test]
    public void TestOverlappingSentenceThrowsException()
    {
        MutableSentence ms = new MutableSentence();
        ms.Add(new Note(ENoteType.Normal, 0, 2, 0, ""));
        mv.Add((Sentence)ms);

        MutableSentence ms2 = new MutableSentence();
        ms2.Add(new Note(ENoteType.Normal, 1, 2, 0, ""));

        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { mv.Add((Sentence)ms2); } );
        Assert.AreEqual("Sentence starts before previous sentence is over", vbe.Message);
    }

    [Test]
    public void TestInterferingLinebreakThrowsException()
    {
        MutableSentence ms = new MutableSentence();
        ms.Add(new Note(ENoteType.Normal, 0, 2, 0, ""));
        ms.SetLinebreakBeat(5);
        mv.Add((Sentence)ms);

        MutableSentence ms2 = new MutableSentence();
        ms2.Add(new Note(ENoteType.Normal, 4, 2, 0, ""));

        VoicesBuilderException vbe = Assert.Throws<VoicesBuilderException>(delegate { mv.Add((Sentence)ms2); } );
        Assert.AreEqual("Sentence conflicts with linebreak of previous sentence", vbe.Message);
    }
}
