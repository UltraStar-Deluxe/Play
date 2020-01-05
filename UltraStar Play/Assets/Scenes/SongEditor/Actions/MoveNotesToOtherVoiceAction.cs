using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;

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

    public void MoveNotesToVoice(SongMeta songMeta, List<Note> selectedNotes, string voiceName)
    {
        Voice voice = SongMetaUtils.GetOrCreateVoice(songMeta, voiceName);
        List<Sentence> changedSentences = new List<Sentence>();
        foreach (Note note in selectedNotes)
        {
            Sentence oldSentence = note.Sentence;
            // Find a sentence in the new voice for the note
            Sentence sentenceForNote = SongMetaUtils.FindExistingSentenceForNote(voice.Sentences, note);
            if (sentenceForNote == null)
            {
                // Create new sentence in the voice.
                // Use the min and max value from the sentence of the original note if possible.
                if (note.Sentence != null)
                {
                    sentenceForNote = new Sentence(note.Sentence.MinBeat, note.Sentence.MaxBeat);
                }
                else
                {
                    sentenceForNote = new Sentence();
                }
                sentenceForNote.SetVoice(voice);
            }
            sentenceForNote.AddNote(note);

            changedSentences.Add(sentenceForNote);
            if (oldSentence != null)
            {
                // Remove old sentence if empty now
                if (oldSentence.Notes.Count == 0 && oldSentence.Voice != null)
                {
                    oldSentence.SetVoice(null);
                }
                else
                {
                    changedSentences.Add(oldSentence);
                }
            }
        }

        // Fit changed sentences to their notes (make them as small as possible)
        foreach (Sentence sentence in changedSentences)
        {
            sentence.FitToNotes();
        }
    }

    public void MoveNotesToVoiceAndNotify(SongMeta songMeta, List<Note> selectedNotes, string voiceName)
    {
        MoveNotesToVoice(songMeta, selectedNotes, voiceName);
        songMetaChangeEventStream.OnNext(new MovedNotesToVoiceEvent(selectedNotes));
    }

    private static bool HasVoice(Note note, string[] voiceNames)
    {
        return note.Sentence != null && voiceNames.Contains(note.Sentence.Voice.Name);
    }

}