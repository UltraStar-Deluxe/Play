using System.Collections.Generic;
using System.Linq;

public static class SongEditorSongMetaUtils
{
    public static List<Note> GetFollowingNotes(SongMeta songMeta, List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        int maxBeat = notes.Select(it => it.EndBeat).Max();
        List<Note> result = SongMetaUtils.GetAllSentences(songMeta)
            .SelectMany(sentence => sentence.Notes)
            .Where(note => note.StartBeat >= maxBeat)
            .ToList();
        return result;
    }

    public static void AddTrailingSpaceToLastNoteOfSentence(Sentence sentence)
    {
        if (sentence == null)
        {
            return;
        }

        AddTrailingSpaceToLastNoteOfSentence(sentence.Notes.LastOrDefault());
    }

    public static void AddTrailingSpaceToLastNoteOfSentence(Note note)
    {
        if (note == null)
        {
            return;
        }

        // Add space at end of note if it was the last note in the sentence. Otherwise, formerly separate words might be merged.
        if (!note.Text.EndsWith(" ")
            && note.Sentence != null
            && note.Sentence.Notes.LastOrDefault() == note)
        {
            note.SetText(note.Text + " ");
        }
    }

    public static void AddVoice(SongMeta songMeta, Voice voice)
    {
        if (songMeta == null
            || voice == null)
        {
            return;
        }

        songMeta.AddVoice(voice);
    }

    public static void RemoveVoice(SongMeta songMeta, EVoiceId voiceId)
    {
        if (songMeta == null)
        {
            return;
        }

        songMeta.RemoveVoice(voiceId);
    }

    public static void RemoveVoice(SongMeta songMeta, Voice voice)
    {
        if (songMeta == null
            || voice == null)
        {
            return;
        }

        RemoveVoice(songMeta, voice.Id);
    }
}
