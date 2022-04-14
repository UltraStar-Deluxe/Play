using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AddNoteAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void Execute(SongMeta songMeta, int beat, int midiNote)
    {
        List<Sentence> sentencesAtBeat = SongMetaUtils.GetSentencesAtBeat(songMeta, beat);
        if (sentencesAtBeat.Count == 0)
        {
            // Add sentence with note
            Note newNote = new(ENoteType.Normal, beat - 2, 4, 0, "~");
            newNote.SetMidiNote(midiNote);
            Sentence newSentence = new(new List<Note> { newNote }, newNote.EndBeat);
            IReadOnlyCollection<Voice> voices = songMeta.GetVoices();
            newSentence.SetVoice(voices.FirstOrDefault());
        }
        else
        {
            // Add note to existing sentence
            Note newNote = new(ENoteType.Normal, beat - 2, 4, 0, "~");
            newNote.SetMidiNote(midiNote);
            newNote.SetSentence(sentencesAtBeat[0]);
        }
    }

    public void ExecuteAndNotify(SongMeta songMeta, int beat, int midiNote)
    {
        Execute(songMeta, beat, midiNote);
        songMetaChangeEventStream.OnNext(new NotesAddedEvent());
    }

}
