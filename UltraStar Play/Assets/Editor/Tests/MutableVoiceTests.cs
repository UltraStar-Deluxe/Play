using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

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
    public void TestOneSentence()
    {
        Note testNote = new Note(ENoteType.Normal, 0, 2, 0, "");
        MutableSentence ms = new MutableSentence();
        ms.Add(testNote);
        mv.Add((Sentence)ms);

        List<Sentence> sentences = mv.GetSentences();
        Assert.AreEqual(1, sentences.Count);
        IReadOnlyCollection<Note> notes = sentences[0].Notes;
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(testNote, notes.FirstOrDefault());
    }
}
