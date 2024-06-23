using System;
using System.Collections.Generic;
using System.Linq;

public static class MoveNotesToOtherVoiceUtils
{
    public static MoveNotesToVoiceResult MoveNotesToVoice(
        SongMeta songMeta,
        List<Note> selectedNotes,
        EVoiceId voiceId)
    {
        Voice targetVoice = SongMetaUtils.GetOrCreateVoice(songMeta, voiceId);
        List<Sentence> changedSentences = new();
        List<Sentence> removedSentences = new();
        List<Sentence> createdSentences = new();

        List<SentenceWithRange> createdSentencesWithRange = new();

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
                Sentence createdSentence = new(newSentenceFromBeat, newSentenceUntilBeat);
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

        return new MoveNotesToVoiceResult(selectedNotes, changedSentences, removedSentences);
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

    public class MoveNotesToVoiceResult
    {
        public IReadOnlyCollection<Note> Notes { get; private set; }
        public IReadOnlyCollection<Sentence> ChangedSentences { get; private set; }
        public IReadOnlyCollection<Sentence> RemovedSentences { get; private set; }

        public MoveNotesToVoiceResult(
            IReadOnlyCollection<Note> notes,
            IReadOnlyCollection<Sentence> changedSentences,
            IReadOnlyCollection<Sentence> removedSentences)
        {
            this.Notes = notes;
            ChangedSentences = changedSentences;
            RemovedSentences = removedSentences;
        }
    }

    public static List<List<Note>> SplitIntoSentences(SongMeta songMeta, List<Note> inputNotes)
    {
        // Copy input list because splitting is done in-place.
        inputNotes = inputNotes.ToList();

        List<List<Note>> result = new() { inputNotes };

        void RemoveEmptyBatches()
        {
            result = result
                .Where(batch => !batch.IsNullOrEmpty())
                .ToList();
        }

        bool CanSplitNote(Note lastNote, Note note)
        {
            // Do not split note if it is part of a word
            return lastNote != null
                    && (lastNote.Text.IsNullOrEmpty()
                        || char.IsWhiteSpace(lastNote.Text.LastOrDefault()));
        }

        /////////////////// Split Batches
        void SplitOnCondition(List<Note> inputBatch, Func<List<Note>, Note, Note, bool> shouldSplitFunction)
        {
            // Remove input batch from the result.
            // The input batch will be split into smaller chunks and these chunks will be added to the result instead.
            result.Remove(inputBatch);

            List<Note> currentBatch = new();
            Note lastNote = null;
            foreach (Note note in inputBatch.ToList())
            {
                if (lastNote != null
                    && !currentBatch.IsNullOrEmpty()
                    && shouldSplitFunction(currentBatch, lastNote, note))
                {
                    result.Add(currentBatch);
                    currentBatch = new List<Note>();
                }

                currentBatch.Add(note);
                inputBatch.Remove(note);

                lastNote = note;
            }

            // Add final batch to result
            if (!currentBatch.IsNullOrEmpty())
            {
                result.Add(currentBatch);
            }
        }

        void SplitOnLongPause(List<Note> inputBatch)
        {
            SplitOnCondition(inputBatch,
                (currentBatch, lastNote, note) => SongMetaUtils.NoteDistanceInMillis(songMeta, note, lastNote) > 1000);
        }
        result.ToList().ForEach(batch => SplitOnLongPause(batch));
        RemoveEmptyBatches();

        void SplitOnLongSentence(List<Note> inputBatch)
        {
            SplitOnCondition(inputBatch,
                (currentBatch, lastNote, note) =>
                {
                    // Split if sentence is long on time.
                    int minBeat = currentBatch.FirstOrDefault().StartBeat;
                    int maxBeat = currentBatch.LastOrDefault().EndBeat;
                    int lengthInBeats = maxBeat - minBeat;
                    double lengthInMillis = lengthInBeats * SongMetaBpmUtils.MillisPerBeat(songMeta);
                    if (lengthInMillis > 10000
                        && CanSplitNote(lastNote, note))
                    {
                        return true;
                    }

                    // Split if sentence is long on text.
                    if (currentBatch.Select(batchNote => batchNote.Text.Length).Sum() > 30
                        // Do not split words across multiple notes
                        && CanSplitNote(lastNote, note))
                    {
                        return true;
                    }

                    return false;
                });
        }
        result.ToList().ForEach(batch => SplitOnLongSentence(batch));
        RemoveEmptyBatches();

        ////////////////////// Merge batches
        void MergeTooShortSentences()
        {
            List<Note> lastBatch = null;
            foreach (List<Note> batch in result.ToList())
            {
                if (lastBatch != null
                    && batch.Count <= 2)
                {
                    // Remove this batch from the result and add its notes to the previous batch instead.
                    result.Remove(batch);
                    lastBatch.AddRange(batch);
                }

                if (!batch.IsNullOrEmpty())
                {
                    lastBatch = batch;
                }
            }
        }
        MergeTooShortSentences();
        RemoveEmptyBatches();

        return result;
    }
}
