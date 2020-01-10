using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SongDataStructureHierarchyTests
{
    [Test]
    public void SetSentenceOnNoteUpdatesOtherReferences()
    {
        Note note = new Note();
        Sentence sentence1 = new Sentence(new List<Note> { note }, 0);
        Assert.AreEqual(1, sentence1.Notes.Count);

        Sentence sentence2 = new Sentence();
        note.SetSentence(sentence2);

        Assert.AreEqual(0, sentence1.Notes.Count);
        Assert.AreEqual(note, sentence2.Notes.FirstOrDefault());
    }

    [Test]
    public void AddNoteToSentenceUpdatesOtherReferences()
    {
        Note note = new Note();
        Sentence sentence1 = new Sentence(new List<Note> { note }, 0);
        Assert.AreEqual(1, sentence1.Notes.Count);

        Sentence sentence2 = new Sentence();
        sentence2.AddNote(note);

        Assert.AreEqual(0, sentence1.Notes.Count);
        Assert.AreEqual(note, sentence2.Notes.FirstOrDefault());
    }

    [Test]
    public void RemoveNoteFromSentenceUpdatesOtherReferences()
    {
        Note note = new Note();
        Sentence sentence1 = new Sentence(new List<Note> { note }, 0);
        Assert.AreEqual(1, sentence1.Notes.Count);

        sentence1.RemoveNote(note);

        Assert.AreEqual(0, sentence1.Notes.Count);
        Assert.IsNull(note.Sentence);
    }

    [Test]
    public void SetVoiceOnSentenceUpdatesOtherReferences()
    {
        Sentence sentence = new Sentence();
        Voice voice1 = new Voice(new List<Sentence> { sentence }, "");
        Assert.AreEqual(1, voice1.Sentences.Count);

        Voice voice2 = new Voice();
        sentence.SetVoice(voice2);

        Assert.AreEqual(0, voice1.Sentences.Count);
        Assert.AreEqual(sentence, voice2.Sentences.FirstOrDefault());
    }

    [Test]
    public void AddSentenceToVoiceUpdatesOtherReferences()
    {
        Sentence sentence = new Sentence();
        Voice voice1 = new Voice(new List<Sentence> { sentence }, "");
        Assert.AreEqual(1, voice1.Sentences.Count);

        Voice voice2 = new Voice();
        voice2.AddSentence(sentence);

        Assert.AreEqual(0, voice1.Sentences.Count);
        Assert.AreEqual(sentence, voice2.Sentences.FirstOrDefault());
    }

    [Test]
    public void RemoveSentenceFromVoiceUpdatesOtherReferences()
    {
        Sentence sentence = new Sentence();
        Voice voice1 = new Voice(new List<Sentence> { sentence }, "");
        Assert.AreEqual(1, voice1.Sentences.Count);

        voice1.RemoveSentence(sentence);

        Assert.AreEqual(0, voice1.Sentences.Count);
        Assert.IsNull(sentence.Voice);
    }

}