using System.Collections.Generic;

public static class SongMetaAnalyzer
{
    public static IReadOnlyCollection<SongIssue> AnalyzeIssues(SongMeta songMeta)
    {
        List<SongIssue> result = new();
        foreach (Voice voice in songMeta.GetVoices())
        {
            AnalyzeSentencesInVoice(songMeta, voice, result);
        }
        return result;
    }

    private static void AnalyzeSentencesInVoice(SongMeta songMeta, Voice voice, List<SongIssue> result)
    {
        // Find overlapping sentences
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        Sentence lastSentence = null;
        foreach (Sentence sentence in sortedSentences)
        {
            if (lastSentence != null && sentence.MinBeat < lastSentence.ExtendedMaxBeat)
            {
                SongIssue issue = SongIssue.CreateError(songMeta, "Sentences overlap", sentence.MinBeat, lastSentence.ExtendedMaxBeat);
                result.Add(issue);
            }
            lastSentence = sentence;

            AnalyzeNotesInSentence(songMeta, sentence, result);
        }
    }

    private static void AnalyzeNotesInSentence(SongMeta songMeta, Sentence sentence, List<SongIssue> result)
    {
        // Find overlapping notes
        List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
        Note lastNote = null;
        foreach (Note note in sortedNotes)
        {
            if (lastNote != null && note.StartBeat < lastNote.EndBeat)
            {
                SongIssue issue = SongIssue.CreateError(songMeta, "Notes overlap", note.StartBeat, lastNote.EndBeat);
                result.Add(issue);
            }

            // Find pitches outside of the singable range
            if (note.MidiNote < MidiUtils.SingableNoteMin || note.MidiNote > MidiUtils.SingableNoteMax)
            {
                SongIssue issue = SongIssue.CreateWarning(songMeta, "Unusual pitch (human range is roughly from C2 to C6).", note.StartBeat, note.EndBeat);
                result.Add(issue);
            }

            // Check that each note has lyrics
            if (note.Text.IsNullOrEmpty())
            {
                SongIssue issue = SongIssue.CreateWarning(songMeta, "Missing lyrics on note", note.StartBeat, note.EndBeat);
                result.Add(issue);
            }

            lastNote = note;
        }
    }
}
