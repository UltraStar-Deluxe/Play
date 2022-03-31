using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SongDataStructureHierarchyTests
{
    [Test]
    public void SetSentenceOnNoteUpdatesOtherReferences()
    {
        Note note = new();
        Sentence sentence1 = new(new List<Note> { note }, 0);
        Assert.AreEqual(1, sentence1.Notes.Count);

        Sentence sentence2 = new();
        note.SetSentence(sentence2);

        Assert.AreEqual(0, sentence1.Notes.Count);
        Assert.AreEqual(note, sentence2.Notes.FirstOrDefault());
    }

    [Test]
    public void AddNoteToSentenceUpdatesOtherReferences()
    {
        Note note = new();
        Sentence sentence1 = new(new List<Note> { note }, 0);
        Assert.AreEqual(1, sentence1.Notes.Count);

        Sentence sentence2 = new();
        sentence2.AddNote(note);

        Assert.AreEqual(0, sentence1.Notes.Count);
        Assert.AreEqual(note, sentence2.Notes.FirstOrDefault());
    }

    [Test]
    public void RemoveNoteFromSentenceUpdatesOtherReferences()
    {
        Note note = new();
        Sentence sentence1 = new(new List<Note> { note }, 0);
        Assert.AreEqual(1, sentence1.Notes.Count);

        sentence1.RemoveNote(note);

        Assert.AreEqual(0, sentence1.Notes.Count);
        Assert.IsNull(note.Sentence);
    }

    [Test]
    public void SetVoiceOnSentenceUpdatesOtherReferences()
    {
        Sentence sentence = new();
        Voice voice1 = new(new List<Sentence> { sentence }, "");
        Assert.AreEqual(1, voice1.Sentences.Count);

        Voice voice2 = new();
        sentence.SetVoice(voice2);

        Assert.AreEqual(0, voice1.Sentences.Count);
        Assert.AreEqual(sentence, voice2.Sentences.FirstOrDefault());
    }

    [Test]
    public void AddSentenceToVoiceUpdatesOtherReferences()
    {
        Sentence sentence = new();
        Voice voice1 = new(new List<Sentence> { sentence }, "");
        Assert.AreEqual(1, voice1.Sentences.Count);

        Voice voice2 = new();
        voice2.AddSentence(sentence);

        Assert.AreEqual(0, voice1.Sentences.Count);
        Assert.AreEqual(sentence, voice2.Sentences.FirstOrDefault());
    }

    [Test]
    public void RemoveSentenceFromVoiceUpdatesOtherReferences()
    {
        Sentence sentence = new();
        Voice voice1 = new(new List<Sentence> { sentence }, "");
        Assert.AreEqual(1, voice1.Sentences.Count);

        voice1.RemoveSentence(sentence);

        Assert.AreEqual(0, voice1.Sentences.Count);
        Assert.IsNull(sentence.Voice);
    }

}