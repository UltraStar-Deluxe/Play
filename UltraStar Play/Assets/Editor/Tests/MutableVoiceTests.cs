using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class MutableVoiceTests
{
    private Voice mv;

    [SetUp]
    public void TestInit()
    {
        mv = new Voice("");
    }

    [Test]
    public void TestCreation()
    {
        Assert.AreEqual(0, mv.Sentences.Count);
    }

    [Test]
    public void TestOneSentence()
    {
        Note testNote = new(ENoteType.Normal, 0, 2, 0, "");
        Sentence ms = new();
        ms.AddNote(testNote);
        mv.AddSentence(ms);

        IReadOnlyCollection<Sentence> sentences = mv.Sentences;
        Assert.AreEqual(1, sentences.Count);
        IReadOnlyCollection<Note> notes = sentences.FirstOrDefault().Notes;
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(testNote, notes.FirstOrDefault());
    }
}
