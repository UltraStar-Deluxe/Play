using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class SongBuilderTests
{
    // ==== MutableSentence ====
    [Test]
    public void TestMutableSentenceCreation()
    {
        MutableSentence ms = new MutableSentence();
        Assert.IsTrue(ms.IsEmpty());
        Assert.AreEqual(0, ms.GetNotes().Count);
        Assert.AreEqual(0, ms.GetLinebreakBeat());
        Assert.Throws<ArgumentOutOfRangeException>(delegate { ms.GetStartBeat(); } );
        Assert.Throws<ArgumentOutOfRangeException>(delegate { ms.GetEndBeat(); } );
    }

    [Test]
    public void TestMutableSentenceNullNoteThrowsException()
    {
        MutableSentence ms = new MutableSentence();
        Assert.Throws<ArgumentNullException>(delegate { ms.AddNote(null); } );
    }

    [Test]
    public void TestMutableSentenceTwoNonOverlappingNotes()
    {
        MutableSentence ms = new MutableSentence();
        ms.AddNote(new Note(0, 1, 1, "note1", ENoteType.Normal));
        ms.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        Assert.AreEqual(2, ms.GetNotes().Count);
        Assert.AreEqual(0, ms.GetLinebreakBeat());
        Assert.AreEqual(1, ms.GetStartBeat());
        Assert.AreEqual(2, ms.GetEndBeat());
        // test will automatically fail if this raises an exception
        ms.AssertValidNotes();
    }

    [Test]
    public void TestMutableSentenceNotesAreSorted()
    {
        MutableSentence ms = new MutableSentence();
        Note note1 = new Note(0, 2, 1, "note1", ENoteType.Normal);
        Note note2 = new Note(0, 1, 1, "note2", ENoteType.Normal);
        ms.AddNote(note1);
        ms.AddNote(note2);
        // notes are not sorted yet
        Assert.AreEqual(2, ms.GetStartBeat());
        Assert.AreEqual(1, ms.GetEndBeat());
        // test will automatically fail if this raises an exception
        ms.AssertValidNotes();
        // notes are sorted now
        Assert.AreEqual(1, ms.GetStartBeat());
        Assert.AreEqual(2, ms.GetEndBeat());
        Assert.AreEqual(0, ms.GetLinebreakBeat());
        List<Note> notes = ms.GetNotes();
        Assert.AreEqual(2, notes.Count);
        Assert.AreEqual(note2, notes[0]);
        Assert.AreEqual(note1, notes[1]);
    }

    [Test]
    public void TestMutableSentenceLinebreakBeat()
    {
        MutableSentence ms = new MutableSentence();
        ms.SetLinebreakBeat(2);
        Assert.AreEqual(0, ms.GetNotes().Count);
        Assert.AreEqual(2, ms.GetLinebreakBeat());
    }

    [Test]
    public void TestMutableSentenceTwoOverlappingNotes()
    {
        MutableSentence ms = new MutableSentence();
        ms.AddNote(new Note(0, 1, 2, "note1", ENoteType.Normal));
        ms.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        Assert.AreEqual(2, ms.GetNotes().Count);
        Assert.AreEqual(0, ms.GetLinebreakBeat());
        Assert.AreEqual(1, ms.GetStartBeat());
        Assert.AreEqual(2, ms.GetEndBeat());
        SongBuilderException sbe = Assert.Throws<SongBuilderException>(delegate { ms.AssertValidNotes(); } );
        Assert.AreEqual("The notes starting at beats 1 and 2 are overlapping", sbe.Message);
    }

    // ==== MutableVoice ====
    [Test]
    public void TestMutableVoiceCreationNullNameThrowsException()
    {
        Assert.Throws<ArgumentNullException>(delegate { new MutableVoice(null); } );
    }

    [Test]
    public void TestMutableVoiceCreation()
    {
        MutableVoice mv = new MutableVoice("somename");
        Assert.AreEqual("somename", mv.GetName());
    }

    [Test]
    public void TestMutableVoiceNullSentenceThrowsException()
    {
        MutableVoice mv = new MutableVoice("somename");
        Assert.Throws<ArgumentNullException>(delegate { mv.AddSentence(null); } );
    }

    [Test]
    public void TestMutableVoiceEmptySentenceDoesNothing()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms = new MutableSentence();
        mv.AddSentence(ms);
        SongBuilderException sbe = Assert.Throws<SongBuilderException>(delegate { mv.AsVoice(); } );
        Assert.AreEqual("Voice 'somename' has no sentences", sbe.Message);
    }

    [Test]
    public void TestMutableVoiceLinebreakShouldBeSetOnAllButLastSentenceSorted()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 1, "note1", ENoteType.Normal));
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms1);
        mv.AddSentence(ms2);
        SongBuilderException sbe = Assert.Throws<SongBuilderException>(delegate { mv.AsVoice(); } );
        Assert.AreEqual("A linebreak should be set for the sentence starting at beat 1", sbe.Message);
    }

    [Test]
    public void TestMutableVoiceLinebreakShouldBeSetOnAllButLastSentenceUnsorted()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 1, "note1", ENoteType.Normal));
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms2);
        mv.AddSentence(ms1);
        SongBuilderException sbe = Assert.Throws<SongBuilderException>(delegate { mv.AsVoice(); } );
        Assert.AreEqual("A linebreak should be set for the sentence starting at beat 1", sbe.Message);
    }

    [Test]
    public void TestMutableVoiceOverlappingSentences()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 2, "note1", ENoteType.Normal));
        ms1.SetLinebreakBeat(3);
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms1);
        mv.AddSentence(ms2);
        SongBuilderException sbe = Assert.Throws<SongBuilderException>(delegate { mv.AsVoice(); } );
        Assert.AreEqual("The sentences starting at beats 1 and 2 are overlapping", sbe.Message);
    }

    [Test]
    public void TestMutableVoiceToVoiceWithUnsortedSentences()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 1, "note1", ENoteType.Normal));
        ms1.SetLinebreakBeat(2);
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms2);
        mv.AddSentence(ms1);
        Voice v = mv.AsVoice();
        Assert.AreEqual("somename", v.GetName());
        List<Sentence> sentences = v.GetSentences();
        Assert.AreEqual(2, sentences.Count);
        Sentence s1 = sentences[0];
        Assert.AreEqual(ms1.GetLinebreakBeat(), s1.GetLinebreakBeat());
        Assert.AreEqual(ms1.GetNotes().Count, s1.GetNotes().Count);
        Assert.AreEqual(ms1.GetNotes()[0], s1.GetNotes()[0]);
        Sentence s2 = sentences[1];
        Assert.AreEqual(ms2.GetLinebreakBeat(), s2.GetLinebreakBeat());
        Assert.AreEqual(ms2.GetNotes().Count, s2.GetNotes().Count);
        Assert.AreEqual(ms2.GetNotes()[0], s2.GetNotes()[0]);
    }

    [Test]
    public void TestMutableVoiceLinebreakMovedOnConsecutiveSentences()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 1, "note1", ENoteType.Normal));
        ms1.SetLinebreakBeat(3);
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 2, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms1);
        mv.AddSentence(ms2);
        Voice v = mv.AsVoice();
        Assert.AreEqual(2, v.GetSentences()[0].GetLinebreakBeat());
    }

    [Test]
    public void TestMutableVoiceLinebreakNotMovedOnGapSentences()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 1, "note1", ENoteType.Normal));
        ms1.SetLinebreakBeat(3);
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 20, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms1);
        mv.AddSentence(ms2);
        Voice v = mv.AsVoice();
        Assert.AreEqual(3, v.GetSentences()[0].GetLinebreakBeat());
    }

    [Test]
    public void TestMutableVoiceLinebreakMovedOnGapSentences()
    {
        MutableVoice mv = new MutableVoice("somename");
        MutableSentence ms1 = new MutableSentence();
        ms1.AddNote(new Note(0, 1, 9, "note1", ENoteType.Normal));
        ms1.SetLinebreakBeat(3);
        MutableSentence ms2 = new MutableSentence();
        ms2.AddNote(new Note(0, 20, 1, "note2", ENoteType.Normal));
        mv.AddSentence(ms1);
        mv.AddSentence(ms2);
        Voice v = mv.AsVoice();
        Assert.AreEqual(13, v.GetSentences()[0].GetLinebreakBeat());
    }
}
