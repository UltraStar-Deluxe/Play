using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class UltraStarFormatWriter
{
    public static void WriteFile(string absolutePath, SongMeta songMeta, bool writeByteOrderMark = true)
    {
        if (songMeta is not UltraStarSongMeta ultraStarSongMeta)
        {
            ultraStarSongMeta = new(songMeta);
        }
        WriteFileWithUltraStarSongMeta(absolutePath, ultraStarSongMeta, writeByteOrderMark);
    }

    private static void WriteFileWithUltraStarSongMeta(string absolutePath, UltraStarSongMeta songMeta, bool writeByteOrderMark)
    {
        string ultraStarFormat = ToUltraStarSongFormat(songMeta);
        File.WriteAllText(absolutePath, ultraStarFormat, EncodingUtils.GetUtf8Encoding(writeByteOrderMark));
    }

    public static string ToUltraStarSongFormat(SongMeta songMeta)
    {
        if (songMeta is not UltraStarSongMeta ultraStarSongMeta)
        {
            ultraStarSongMeta = new(songMeta);
        }
        return ToUltraStarSongFormat(ultraStarSongMeta);
    }

    private static string ToUltraStarSongFormat(UltraStarSongMeta songMeta)
    {
        StringBuilder sb = new();
        AppendHeader(sb, songMeta);
        List<Voice> nonEmptyVoices = songMeta.Voices.Where(voice => IsNotEmpty(voice)).ToList();
        nonEmptyVoices.Sort(Voice.comparerById);
        foreach (Voice voice in nonEmptyVoices)
        {
            AppendVoice(sb, voice);
        }
        sb.Append("E");
        return sb.ToString();
    }

    private static void AppendVoice(StringBuilder sb, Voice voice)
    {
        sb.AppendLine(voice.Id.ToString());
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

    public static string GetNoteTypePrefix(ENoteType noteType)
    {
        switch (noteType)
        {
            case ENoteType.Normal: return ":";
            case ENoteType.Golden: return "*";
            case ENoteType.Freestyle: return "F";
            case ENoteType.Rap: return "R";
            case ENoteType.RapGolden: return "G";
            default:
                throw new UltraStarSongWriterException($"Unknown note type '{noteType}'.");
        }
    }

    private static void AppendHeader(StringBuilder sb, UltraStarSongMeta songMeta)
    {
        AppendHeaderField(sb, "title", songMeta.Title);
        AppendHeaderField(sb, "artist", songMeta.Artist);
        AppendHeaderField(sb, "mp3", songMeta.Audio);
        AppendHeaderField(sb, "Vocals", songMeta.VocalsAudio);
        AppendHeaderField(sb, "Instrumental", songMeta.InstrumentalAudio);
        AppendNumberHeaderField(sb, "bpm", songMeta.TxtFileBpm);
        AppendNumberHeaderField(sb, "gap", songMeta.GapInMillis);

        AppendHeaderField(sb, "cover", songMeta.Cover);
        AppendHeaderField(sb, "background", songMeta.Background);

        AppendHeaderField(sb, "video", songMeta.Video);
        AppendNumberHeaderField(sb, "videogap", songMeta.TxtFileVideoGapInSeconds);

        AppendHeaderField(sb, "website", songMeta.Website);

        AppendHeaderField(sb, "genre", songMeta.Genre);
        AppendNumberHeaderField(sb, "year", songMeta.Year);

        AppendHeaderField(sb, "language", songMeta.Language);
        AppendHeaderField(sb, "edition", songMeta.Edition);

        AppendNumberHeaderField(sb, "start", songMeta.TxtFileStartInSeconds);
        AppendNumberHeaderField(sb, "end", songMeta.TxtFileEndInMillis);
        AppendNumberHeaderField(sb, "previewstart", songMeta.TxtFilePreviewStartInSeconds);
        AppendNumberHeaderField(sb, "previewend", songMeta.TxtFilePreviewEndInSeconds);
        AppendNumberHeaderField(sb, "medleystartbeat", (int)songMeta.TxtFileMedleyStartBeat);
        AppendNumberHeaderField(sb, "medleyendbeat", (int)songMeta.TxtFileMedleyEndBeat);

        songMeta.AdditionalHeaderEntries.ForEach(entry =>
            AppendHeaderField(sb, entry.Key, entry.Value));
    }

    private static void AppendHeaderField(StringBuilder sb, string key, string value)
    {
        if (!value.IsNullOrEmpty())
        {
            sb.AppendLine($"#{key.ToUpper(CultureInfo.InvariantCulture)}:{value}");
        }
    }

    private static void AppendNumberHeaderField(StringBuilder sb, string key, int value)
    {
        if (value != 0)
        {
            AppendHeaderField(sb, key, value.ToString());
        }
    }

    private static void AppendNumberHeaderField(StringBuilder sb, string key, double value)
    {
        if (value != 0)
        {
            AppendHeaderField(sb, key, value.ToStringInvariantCulture());
        }
    }
}
