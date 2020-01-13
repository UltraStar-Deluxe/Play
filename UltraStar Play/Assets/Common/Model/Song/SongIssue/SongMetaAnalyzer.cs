using System;
using System.Collections.Generic;

public static class SongMetaAnalyzer
{
    public static IReadOnlyCollection<SongIssue> AnalyzeIssues(SongMeta songMeta)
    {
        List<SongIssue> result = new List<SongIssue>();
        foreach (Voice voice in songMeta.GetVoices())
        {
            AnalyzeSentencesInVoice(voice, result);
        }
        return result;
    }

    private static void AnalyzeSentencesInVoice(Voice voice, List<SongIssue> result)
    {
        // Find overlapping sentences
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        Sentence lastSentence = null;
        foreach (Sentence sentence in sortedSentences)
        {
            if (lastSentence != null && sentence.MinBeat < lastSentence.ExtendedMaxBeat)
            {
                SongIssue issue = SongIssue.CreateError("Sentences overlap", sentence.MinBeat, lastSentence.ExtendedMaxBeat);
                result.Add(issue);
            }
            lastSentence = sentence;

            AnalyzeNotesInSentence(sentence, result);
        }
    }

    private static void AnalyzeNotesInSentence(Sentence sentence, List<SongIssue> result)
    {
        // Find overlapping notes
        List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
        Note lastNote = null;
        foreach (Note note in sortedNotes)
        {
            if (lastNote != null && note.StartBeat < lastNote.EndBeat)
            {
                SongIssue issue = SongIssue.CreateError("Notes overlap", note.StartBeat, lastNote.EndBeat);
                result.Add(issue);
            }

            // Find pitches outside of the singable range
            if (note.MidiNote < MidiUtils.MidiNoteMin || note.MidiNote > MidiUtils.MidiNoteMax)
            {
                SongIssue issue = SongIssue.CreateWarning("Unusual pitch (human range is roughly from C2 to C6).", note.StartBeat, note.EndBeat);
                result.Add(issue);
            }

            lastNote = note;
        }
    }
}