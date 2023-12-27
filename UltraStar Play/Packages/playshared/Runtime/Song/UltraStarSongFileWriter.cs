using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class UltraStarSongFileWriter
{
    public static void WriteFile(string absolutePath, SongMeta songMeta)
    {
        string txtFileContent = GetTxtFileContent(songMeta);
        // Do not use Encoding.UTF8 as it will produce a BOM.
        File.WriteAllText(absolutePath, txtFileContent, new UTF8Encoding());
    }

    private static string GetTxtFileContent(SongMeta songMeta)
    {
        StringBuilder sb = new();
        AppendHeader(sb, songMeta);
        List<Voice> nonEmptyVoices = songMeta.GetVoices().Where(voice => IsNotEmpty(voice)).ToList();
        if (nonEmptyVoices.Count == 0)
        {
            throw new UltraStarSongFileWriterException("The song does not contain any notes");
        }
        nonEmptyVoices.Sort(Voice.comparerByName);
        foreach (Voice voice in nonEmptyVoices)
        {
            string voiceName = voice.Name;
            if (nonEmptyVoices.Count == 1)
            {
                voiceName = Voice.soloVoiceName;
            }
            else if (voiceName == Voice.soloVoiceName)
            {
                voiceName = Voice.firstVoiceName;
            }

            AppendVoice(sb, voice, voiceName);
        }
        sb.Append("E");
        return sb.ToString();
    }

    private static void AppendVoice(StringBuilder sb, Voice voice, string voiceName)
    {
        if (!voiceName.IsNullOrEmpty())
        {
            sb.AppendLine(voiceName);
        }
        List<Sentence> sortedSentences = new(voice.Sentences);
        sortedSentences.Sort(Sentence.comparerByStartBeat);
        foreach (Sentence sentence in sortedSentences)
        {
            AppendSentence(sb, sentence);
        }
    }

    private static void AppendSentence(StringBuilder sb, Sentence sentence)
    {
        bool isEmpty = sentence.Notes.Count == 0;
        if (isEmpty)
        {
            return;
        }

        List<Note> sortedNotes = new(sentence.Notes);
        sortedNotes.Sort(Note.comparerByStartBeat);
        foreach (Note note in sortedNotes)
        {
            AppendNote(sb, note);
        }
        sb.AppendLine($"- {sentence.ExtendedMaxBeat}");
    }

    private static bool IsNotEmpty(Voice voice)
    {
        return voice.Sentences.SelectMany(sentence => sentence.Notes).Any();
    }

    private static void AppendNote(StringBuilder sb, Note note)
    {
        if (note.Length == 0)
        {
            return;
        }

        sb.AppendLine($"{GetNoteTypePrefix(note.Type)} {note.StartBeat} {note.Length} {note.TxtPitch} {note.Text}");
    }

    private static string GetNoteTypePrefix(ENoteType noteType)
    {
        switch (noteType)
        {
            case ENoteType.Normal: return ":";
            case ENoteType.Golden: return "*";
            case ENoteType.Freestyle: return "F";
            case ENoteType.Rap: return "R";
            case ENoteType.RapGolden: return "G";
            default:
                throw new UltraStarSongFileWriterException("Unkown note type '" + noteType + "'.");
        }
    }

    private static void AppendHeader(StringBuilder sb, SongMeta songMeta)
    {
        AppendHeaderField(sb, "encoding", "UTF8");

        AppendHeaderField(sb, "title", songMeta.Title);
        AppendHeaderField(sb, "artist", songMeta.Artist);
        AppendHeaderField(sb, "mp3", songMeta.Mp3);
        AppendHeaderField(sb, "bpm", songMeta.Bpm.ToString(CultureInfo.InvariantCulture));
        if (songMeta.Gap != 0)
        {
            AppendHeaderField(sb, "gap", songMeta.Gap.ToString(CultureInfo.InvariantCulture));
        }

        AppendHeaderField(sb, "cover", songMeta.Cover);
        AppendHeaderField(sb, "background", songMeta.Background);

        AppendHeaderField(sb, "video", songMeta.Video);
        if (songMeta.VideoGap != 0)
        {
            AppendHeaderField(sb, "videogap", songMeta.VideoGap.ToString(CultureInfo.InvariantCulture));
        }

        AppendHeaderField(sb, "genre", songMeta.Genre);
        if (songMeta.Year > 0)
        {
            AppendHeaderField(sb, "year", songMeta.Year.ToString());
        }

        AppendHeaderField(sb, "language", songMeta.Language);
        AppendHeaderField(sb, "edition", songMeta.Edition);

        if (songMeta.Start != 0)
        {
            AppendHeaderField(sb, "start", songMeta.Start.ToString(CultureInfo.InvariantCulture));
        }
        if (songMeta.End != 0)
        {
            AppendHeaderField(sb, "end", songMeta.End.ToString(CultureInfo.InvariantCulture));
        }
        if (songMeta.PreviewStart != 0)
        {
            AppendHeaderField(sb, "previewstart", songMeta.PreviewStart.ToString(CultureInfo.InvariantCulture));
        }
        if (songMeta.PreviewEnd != 0)
        {
            AppendHeaderField(sb, "previewend", songMeta.PreviewEnd.ToString(CultureInfo.InvariantCulture));
        }

        songMeta.UnknownHeaderEntries.ForEach(entry => AppendHeaderField(sb, entry.Key, entry.Value));
    }

    private static void AppendHeaderField(StringBuilder sb, string key, string value)
    {
        if (!value.IsNullOrEmpty())
        {
            sb.AppendLine($"#{key.ToUpper(CultureInfo.InvariantCulture)}:{value}");
        }
    }
}
