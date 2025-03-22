using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SongIdComputer
{
    public static string ComputeScoreRelevantSongHash(SongMeta songMeta)
    {
        StringBuilder sb = new();
        sb.Append("{");

        sb.Append("BPM:");
        sb.Append(songMeta.BeatsPerMinute.ToStringInvariantCulture("0.00"));

        int voiceIndex = 1;
        List<Voice> sortedVoices = songMeta.Voices
            .OrderBy(voice => voice.Id)
            .ToList();
        foreach (Voice voice in sortedVoices)
        {
            sb.Append("|");
            sb.Append("P");
            sb.Append(voiceIndex);

            IEnumerable<Note> scoreRelevantNotes = voice.Sentences.SelectMany(sentence => sentence.Notes)
                .Where(n => n.Type is not ENoteType.Freestyle)
                .OrderBy(n => n.StartBeat);
            foreach (Note note in scoreRelevantNotes)
            {
                sb.Append("|");
                sb.Append(UltraStarFormatWriter.GetNoteTypePrefix(note.Type));
                sb.Append(" ");
                sb.Append(note.StartBeat);
                sb.Append(" ");
                sb.Append(note.Length);
                sb.Append(" ");
                sb.Append(note.TxtPitch);
            }

            voiceIndex++;
        }

        sb.Append("}");

        string scoreRelevantSongHash = Hashing.Md5(Encoding.UTF8.GetBytes(sb.ToString()));
        Log.Verbose(() => $"{songMeta} has score relevant hash '{scoreRelevantSongHash}', computed from string: {sb}");
        return scoreRelevantSongHash;
    }

    public static string ComputeUniqueSongHash(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }

        string ultraStarSongFormat = UltraStarFormatWriter.ToUltraStarSongFormat(songMeta);
        string songHash = Hashing.Md5(Encoding.UTF8.GetBytes(ultraStarSongFormat));
        return songHash;
    }
}
