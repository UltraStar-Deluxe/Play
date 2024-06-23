using System.Collections.Generic;
using System.Linq;

public static class SongMetaAnalyzer
{
    public static IReadOnlyCollection<SongIssue> AnalyzeIssues(
        SongMeta songMeta,
        int maxSongIssueCountPerMessage)
    {
        Dictionary<string, List<SongIssue>> messageToIssues = new();
        foreach (Voice voice in songMeta.Voices)
        {
            AnalyzeSentencesInVoice(songMeta, voice, messageToIssues, maxSongIssueCountPerMessage);
        }
        return messageToIssues.Values
            .SelectMany(songIssues => songIssues)
            .ToList();
    }

    private static void AnalyzeSentencesInVoice(
        SongMeta songMeta,
        Voice voice,
        Dictionary<string, List<SongIssue>> messageToIssues,
        int maxSongIssueCountPerMessage)
    {
        // Find overlapping sentences
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        Sentence lastSentence = null;
        foreach (Sentence sentence in sortedSentences)
        {
            if (lastSentence != null && sentence.MinBeat < lastSentence.ExtendedMaxBeat)
            {
                SongIssue issue = SongIssue.CreateError(songMeta, Translation.Get("songIssue_sentencesOverlap"), sentence.MinBeat, lastSentence.ExtendedMaxBeat);
                AddSongIssue(messageToIssues, issue, maxSongIssueCountPerMessage);
            }
            lastSentence = sentence;

            AnalyzeNotesInSentence(songMeta, sentence, messageToIssues, maxSongIssueCountPerMessage);
        }
    }

    private static void AnalyzeNotesInSentence(
        SongMeta songMeta,
        Sentence sentence,
        Dictionary<string, List<SongIssue>> messageToIssues,
        int maxSongIssueCountPerMessage)
    {
        List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
        Note lastNote = null;
        foreach (Note note in sortedNotes)
        {
            // Find overlapping notes
            if (lastNote != null && note.StartBeat < lastNote.EndBeat)
            {
                SongIssue issue = SongIssue.CreateError(songMeta, Translation.Get("songIssue_notesOverlap"), note.StartBeat, lastNote.EndBeat);
                AddSongIssue(messageToIssues, issue, maxSongIssueCountPerMessage);
            }

            // Find pitches outside of the singable range
            if (note.MidiNote < MidiUtils.SingableNoteMin || note.MidiNote > MidiUtils.SingableNoteMax)
            {
                SongIssue issue = SongIssue.CreateWarning(songMeta, Translation.Get("songIssue_unusualPitch"), note.StartBeat, note.EndBeat);
                AddSongIssue(messageToIssues, issue, maxSongIssueCountPerMessage);
            }

            // Check that each note has lyrics
            if (note.Text.IsNullOrEmpty())
            {
                SongIssue issue = SongIssue.CreateWarning(songMeta, Translation.Get("songIssue_missingLyricsOnNote"), note.StartBeat, note.EndBeat);
                AddSongIssue(messageToIssues, issue, maxSongIssueCountPerMessage);
            }

            lastNote = note;
        }
    }

    private static void AddSongIssue(
        Dictionary<string, List<SongIssue>> messageToIssues,
        SongIssue songIssue,
        int maxSongIssueCountPerMessage)
    {
        string message = songIssue.Message;
        int songIssueCount = GetSongIssueCount(messageToIssues, message);
        if (songIssueCount >= maxSongIssueCountPerMessage)
        {
            return;
        }

        if (songIssueCount <= 0)
        {
            messageToIssues[message] = new List<SongIssue>();
        }
        messageToIssues[message].Add(songIssue);
    }

    private static int GetSongIssueCount(
        Dictionary<string, List<SongIssue>> messageToIssues,
        string message)
    {
        if (messageToIssues.TryGetValue(message, out List<SongIssue> existingIssues))
        {
            return existingIssues.Count;
        }

        return 0;
    }
}
