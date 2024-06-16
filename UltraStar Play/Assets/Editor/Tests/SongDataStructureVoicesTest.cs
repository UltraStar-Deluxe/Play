using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SongDataStructureVoicesTest
{
    public class EditHierarchyTest
    {
        [Test]
        public void SentenceMinAndMaxBeatIsSetWhenCreated()
        {
            Note n2_4 = new(ENoteType.Normal, 2, 2, 0, "");

            // MinBeat and MaxBeat are set when the sentence is directly created with notes.
            Sentence s1 = new(new List<Note> { n2_4 }, 0);
            Assert.AreEqual(2, s1.MinBeat);
            Assert.AreEqual(4, s1.MaxBeat);

            // MinBeat and MaxBeat are set when the sentence is created empty and notes are added.
            Sentence s2 = new();
            s2.AddNote(n2_4);
            Assert.AreEqual(2, s2.MinBeat);
            Assert.AreEqual(4, s2.MaxBeat);
        }

        [Test]
        public void SentenceMinAndMaxBeatChangeWithNotes()
        {
            Note n0_2 = new(ENoteType.Normal, 0, 2, 0, "");
            Note n2_4 = new(ENoteType.Normal, 2, 2, 0, "");
            Note n4_6 = new(ENoteType.Normal, 4, 2, 0, "");

            Sentence s1 = new(new List<Note> { n2_4 }, 0);
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
            Note n2_4 = new(ENoteType.Normal, 2, 2, 0, "");

            Sentence s1 = new(new List<Note> { n2_4 }, 0);
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
            Voice voice1 = new(EVoiceId.P1, new List<Sentence> { sentence });
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
            Voice voice1 = new(EVoiceId.P1, new List<Sentence> { sentence });
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
            Voice voice1 = new(EVoiceId.P1, new List<Sentence> { sentence });
            Assert.AreEqual(1, voice1.Sentences.Count);

            voice1.RemoveSentence(sentence);

            Assert.AreEqual(0, voice1.Sentences.Count);
            Assert.IsNull(sentence.Voice);
        }
    }

    public class EditSentenceTest
    {
        private Sentence sentence;

        [SetUp]
        public void SetUp()
        {
            sentence = new Sentence();
        }

        [Test]
        public void InitiallyEmpty()
        {
            Assert.AreEqual(0, sentence.Notes.Count);
            Assert.AreEqual(0, sentence.LinebreakBeat);
        }

        [Test]
        public void CannotAddNullNote()
        {
            Assert.Throws<ArgumentNullException>(delegate { sentence.AddNote(null); });
        }

        [Test]
        public void SetLinebreakBeat()
        {
            sentence.SetLinebreakBeat(2);
            Assert.AreEqual(2, sentence.LinebreakBeat);
        }

        [Test]
        public void AddNote()
        {
            Note testNote = new(ENoteType.Normal, 0, 2, 0, "");
            sentence.AddNote(testNote);
            IReadOnlyCollection<Note> notes = sentence.Notes;
            Assert.AreEqual(1, notes.Count);
            Assert.AreEqual(testNote, notes.FirstOrDefault());
        }
    }

    public class EditVoicesTest
    {
        private Voice voice;

        [SetUp]
        public void SetUp()
        {
            voice = new Voice(EVoiceId.P1);
        }

        [Test]
        public void InitiallyEmpty()
        {
            Assert.AreEqual(0, voice.Sentences.Count);
        }

        [Test]
        public void AddSentenceWithNote()
        {
            Note testNote = new(ENoteType.Normal, 0, 2, 0, "");
            Sentence ms = new();
            ms.AddNote(testNote);
            voice.AddSentence(ms);

            IReadOnlyCollection<Sentence> sentences = voice.Sentences;
            Assert.AreEqual(1, sentences.Count);
            IReadOnlyCollection<Note> notes = sentences.FirstOrDefault().Notes;
            Assert.AreEqual(1, notes.Count);
            Assert.AreEqual(testNote, notes.FirstOrDefault());
        }
    }
}
