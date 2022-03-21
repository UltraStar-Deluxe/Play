using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.Assertions.Must;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNotesToOtherVoiceAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    // The notes can be moved if there exists a note
    // that is not yet inside a voice with one of the given voice names.
    public bool CanMoveNotesToVoice(List<Note> selectedNotes, params string[] voiceNames)
    {
        return selectedNotes.AnyMatch(note => !HasVoice(note, voiceNames));
    }

    public MovedNotesToVoiceEvent MoveNotesToVoice(SongMeta songMeta, List<Note> selectedNotes, string voiceName)
    {
        Voice targetVoice = SongMetaUtils.GetOrCreateVoice(songMeta, voiceName);
        List<Sentence> changedSentences = new List<Sentence>();
        List<Sentence> removedSentences = new List<Sentence>();
        List<Sentence> createdSentences = new List<Sentence>();

        List<SentenceWithRange> createdSentencesWithRange = new List<SentenceWithRange>();

        List<Sentence> sortedTargetSentences = targetVoice.Sentences.ToList();
        sortedTargetSentences.Sort(Sentence.comparerByStartBeat);

        selectedNotes.Sort(Note.comparerByStartBeat);
        selectedNotes.ForEach(note =>
        {
            Sentence oldSentence = note.Sentence;
            // Find or create a sentence in the target voice for the note
            Sentence targetSentence;
            Sentence existingTargetSentence = SongMetaUtils.FindExistingSentenceForNote(sortedTargetSentences, note);
            if (existingTargetSentence == null)
            {
                SentenceWithRange existingSentenceWithRange = createdSentencesWithRange
                    .FirstOrDefault(sentenceWithRange => sentenceWithRange.ContainsBeatRange(note.StartBeat, note.EndBeat));
                if (existingSentenceWithRange != null)
                {
                    existingTargetSentence = existingSentenceWithRange.Sentence;
                }
            }

            if (existingTargetSentence != null)
            {
                targetSentence = existingTargetSentence;
            }
            else
            {
                // Create sentence to fill the gap between adjacent sentences.
                Sentence previousSentence = sortedTargetSentences
                    .LastOrDefault(sentence => sentence.MaxBeat < note.StartBeat);
                Sentence nextSentence = sortedTargetSentences
                    .LastOrDefault(sentence => sentence.MinBeat > note.EndBeat);
                int newSentenceFromBeat = previousSentence != null
                    ? previousSentence.ExtendedMaxBeat
                    : int.MinValue;
                int newSentenceUntilBeat = nextSentence != null
                    ? nextSentence.MinBeat
                    : int.MaxValue;
                Sentence createdSentence = new Sentence(newSentenceFromBeat, newSentenceUntilBeat);
                createdSentence.SetVoice(targetVoice);

                createdSentences.Add(createdSentence);
                sortedTargetSentences.Add(createdSentence);
                sortedTargetSentences.Sort(Sentence.comparerByStartBeat);

                // Remember this sentence with its full range (that fills the gap between adjacent sentences)
                // The MinBeat and MaxBeat of the sentence will change to fit the notes,
                // such that the original range has to be stored in a dedicated data structure.
                createdSentencesWithRange.Add(new SentenceWithRange(createdSentence, newSentenceFromBeat, newSentenceUntilBeat));

                targetSentence = createdSentence;
            }
            targetSentence.AddNote(note);

            // Set lyrics if none yet (otherwise, there is a warning because of missing lyrics)
            if (note.Text.IsNullOrEmpty())
            {
                note.SetText(" ");
            }

            changedSentences.Add(targetSentence);
            if (oldSentence != null)
            {
                // Remove old sentence if empty now
                if (oldSentence.Notes.Count == 0 && oldSentence.Voice != null)
                {
                    removedSentences.Add(oldSentence);
                    oldSentence.SetVoice(null);
                }
                else
                {
                    changedSentences.Add(oldSentence);
                }
            }
        });

        // Fit sentences to their notes (make them as small as possible)
        changedSentences
            .Union(createdSentences)
            .ForEach(sentence =>
        {
            sentence.FitToNotes();
        });

        return new MovedNotesToVoiceEvent(selectedNotes, changedSentences, removedSentences);
    }

    public void MoveNotesToVoiceAndNotify(SongMeta songMeta, List<Note> selectedNotes, string voiceName)
    {
        MovedNotesToVoiceEvent movedNotesToVoiceEvent = MoveNotesToVoice(songMeta, selectedNotes, voiceName);
        songMetaChangeEventStream.OnNext(movedNotesToVoiceEvent);
    }

    private static bool HasVoice(Note note, string[] voiceNames)
    {
        if (voiceNames.IsNullOrEmpty()
            || note == null)
        {
            return false;
        }
        return note.Sentence != null
               && voiceNames.AnyMatch(voiceName => note.Sentence.Voice.VoiceNameEquals(voiceName));
    }

    private class SentenceWithRange
    {
        public Sentence Sentence { get; private set; }
        public int FromBeat { get; private set; }
        public int UntilBeat { get; private set; }

        public SentenceWithRange(Sentence sentence, int fromBeat, int untilBeat)
        {
            Sentence = sentence;
            FromBeat = fromBeat;
            UntilBeat = untilBeat;
        }

        public bool ContainsBeatRange(int fromBeat, int untilBeat)
        {
            return FromBeat <= fromBeat
                   && untilBeat <= UntilBeat;
        }
    }
}
