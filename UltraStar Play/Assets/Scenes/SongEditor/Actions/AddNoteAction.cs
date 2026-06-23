using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AddNoteAction : INeedInjection
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    public void Execute(SongMeta songMeta, int beat, int midiNote, int lengthInBeats = 4)
    {
        List<Sentence> sentencesAtBeat = SongMetaUtils.GetSentencesAtBeat(songMeta, beat);
        if (sentencesAtBeat.Count == 0)
        {
            // Add sentence with note
            Note newNote = new(ENoteType.Normal, beat, lengthInBeats, 0, "~");
            newNote.SetMidiNote(midiNote);
            Sentence newSentence = new(new List<Note> { newNote }, newNote.EndBeat);
            IReadOnlyCollection<Voice> voices = songMeta.Voices;
            newSentence.SetVoice(voices.FirstOrDefault());
        }
        else
        {
            // Add note to existing sentence
            Note newNote = new(ENoteType.Normal, beat, lengthInBeats, 0, "~");
            newNote.SetMidiNote(midiNote);
            newNote.SetSentence(sentencesAtBeat[0]);
        }
    }

    public void ExecuteAndNotify(SongMeta songMeta, int beat, int midiNote, int lengthInBeats = 4)
    {
        Execute(songMeta, beat, midiNote, lengthInBeats);
        songMetaChangedEventStream.OnNext(new NotesAddedEvent());
    }

}
