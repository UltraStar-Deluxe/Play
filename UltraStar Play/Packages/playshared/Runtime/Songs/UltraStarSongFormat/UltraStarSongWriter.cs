using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class UltraStarFormatWriter
{
    public static void WriteFile(string absolutePath, SongMeta songMeta)
    {
        if (songMeta is not UltraStarSongMeta ultraStarSongMeta)
        {
            ultraStarSongMeta = new(songMeta);
        }
        WriteFile(absolutePath, ultraStarSongMeta);
    }

    private static void WriteFile(string absolutePath, UltraStarSongMeta songMeta)
    {
        string ultraStarFormat = ToUltraStarSongFormat(songMeta);
        File.WriteAllText(absolutePath, ultraStarFormat, Encoding.UTF8);
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
        AppendHeaderField(sb, "encoding", "UTF8");

        AppendHeaderField(sb, "title", songMeta.Title);
        AppendHeaderField(sb, "artist", songMeta.Artist);
        AppendHeaderField(sb, "mp3", songMeta.Audio);
        AppendHeaderField(sb, "VocalsAudio", songMeta.VocalsAudio);
        AppendHeaderField(sb, "InstrumentalAudio", songMeta.InstrumentalAudio);
        AppendHeaderField(sb, "MusicBrainzRecord", songMeta.MusicBrainzRecord);
        AppendHeaderField(sb, "MusicBrainzRelease", songMeta.MusicBrainzRelease);
        AppendHeaderField(sb, "MusicBrainzReleaseGroup", songMeta.MusicBrainzReleaseGroup);
        AppendHeaderField(sb, "MusicBrainzArtist", songMeta.MusicBrainzArtist);
        AppendHeaderField(sb, "bpm", songMeta.TxtFileBpm.ToString(CultureInfo.InvariantCulture));
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

        if (songMeta.MedleyStartBeat != 0)
        {
            AppendHeaderField(sb, "medleystartbeat", songMeta.MedleyStartBeat.ToString(CultureInfo.InvariantCulture));
        }
        if (songMeta.MedleyEndBeat != 0)
        {
            AppendHeaderField(sb, "medleyendbeat", songMeta.MedleyEndBeat.ToString(CultureInfo.InvariantCulture));
        }

        songMeta.AdditionalHeaderEntries.ForEach(entry => AppendHeaderField(sb, entry.Key, entry.Value));
    }

    private static void AppendHeaderField(StringBuilder sb, string key, string value)
    {
        if (!value.IsNullOrEmpty())
        {
            sb.AppendLine($"#{key.ToUpper(CultureInfo.InvariantCulture)}:{value}");
        }
    }
}
