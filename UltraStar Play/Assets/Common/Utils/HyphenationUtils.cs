using System.Collections.Generic;
using NHyphenator;

public static class HyphenationUtils
{
    public static void SplitNotesByHyphenation(
        SongMeta songMeta,
        Settings settings,
        NoteHyphenator noteHyphenator,
        List<Note> notes)
    {
        Hyphenator hyphenator = settings.SongEditorSettings.SplitSyllablesAfterAiTools
            ? SettingsUtils.CreateHyphenator(settings)
            : null;
        if (hyphenator == null)
        {
            return;
        }

        Dictionary<Note,List<Note>> noteToNotesAfterSplit = noteHyphenator.HypenateNotes(songMeta, notes, hyphenator);
        noteToNotesAfterSplit.ForEach(entry =>
        {
            Note note = entry.Key;
            List<Note> notesAfterSplit = entry.Value;
            List<Note> newNotes = new List<Note>(notesAfterSplit);
            newNotes.Remove(note);
            notes.AddRange(newNotes);
        });
    }
}
