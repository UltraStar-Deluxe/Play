using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class UltraStarFormatWriter
{
    public static void WriteFile(string absolutePath, SongMeta songMeta, UltraStarSongFormatVersion version, bool writeByteOrderMark = true)
    {
        if (version.EnumValue is EUltraStarSongFormatVersion.Unknown)
        {
            throw new ArgumentException("Must specify a known UltraStar format version");
        }

        if (songMeta is not UltraStarSongMeta ultraStarSongMeta)
        {
            ultraStarSongMeta = new UltraStarSongMeta(songMeta);
        }
        WriteFileWithUltraStarSongMeta(absolutePath, ultraStarSongMeta, version, writeByteOrderMark);
    }

    private static void WriteFileWithUltraStarSongMeta(string absolutePath, UltraStarSongMeta songMeta, UltraStarSongFormatVersion version, bool writeByteOrderMark)
    {
        string ultraStarFormat = ToUltraStarSongFormat(songMeta, version);
        File.WriteAllText(absolutePath, ultraStarFormat, EncodingUtils.GetUtf8Encoding(writeByteOrderMark));
    }

    public static string ToUltraStarSongFormat(SongMeta songMeta)
    {
        if (songMeta is not UltraStarSongMeta ultraStarSongMeta)
        {
            ultraStarSongMeta = new UltraStarSongMeta(songMeta);
        }
        return ToUltraStarSongFormat(ultraStarSongMeta, ultraStarSongMeta.Version);
    }

    private static string ToUltraStarSongFormat(UltraStarSongMeta songMeta, UltraStarSongFormatVersion version)
    {
        StringBuilder sb = new();
        AppendHeader(sb, songMeta, version);
        List<Voice> nonEmptyVoices = songMeta.Voices.Where(voice => IsNotEmpty(voice)).ToList();
        nonEmptyVoices.Sort(Voice.comparerById);
        foreach (Voice voice in nonEmptyVoices)
        {
            AppendVoice(sb, voice, nonEmptyVoices.Count > 1);
        }
        sb.Append("E");
        return sb.ToString();
    }

    private static void AppendVoice(StringBuilder sb, Voice voice, bool appendVoiceId)
    {
        if (appendVoiceId)
        {
            // P1 is optional when only having one voice
            sb.AppendLine(voice.Id.ToString());
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

        // TODO: Linebreak timing could be optional but is required by some other tools, https://github.com/UltraStar-Deluxe/format/issues/64
        sb.AppendLine($"- {sentence.ExtendedMaxBeat}");
        // if (sentence.ExtendedMaxBeat > sentence.MaxBeat)
        // {
        //     sb.AppendLine($"- {sentence.ExtendedMaxBeat}");
        // } else {
        //     sb.AppendLine($"-");
        // }
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

    private static void AppendHeader(StringBuilder sb, UltraStarSongMeta songMeta, UltraStarSongFormatVersion version)
    {
        AppendHeaderField(sb, "version", version.StringValue);

        AppendHeaderField(sb, "title", songMeta.Title);
        AppendHeaderField(sb, "artist", songMeta.Artist);

        AppendHeaderField(sb, version.IsBefore(UltraStarSongFormatVersion.v110) ? "mp3" : "audio", songMeta.Audio);
        AppendHeaderField(sb, "audiourl", songMeta.AudioUrl);

        AppendHeaderField(sb, "vocals", songMeta.VocalsAudio);
        AppendHeaderField(sb, "vocalsurl", songMeta.VocalsAudioUrl);

        AppendHeaderField(sb, "instrumental", songMeta.InstrumentalAudio);
        AppendHeaderField(sb, "instrumentalurl", songMeta.InstrumentalAudioUrl);

        AppendNumberHeaderField(sb, "bpm", songMeta.TxtFileBpm);
        AppendNumberHeaderField(sb, "gap", songMeta.GapInMillis);

        AppendHeaderField(sb, "cover", songMeta.Cover);
        AppendHeaderField(sb, "coverurl", songMeta.CoverUrl);

        AppendHeaderField(sb, "background", songMeta.Background);
        AppendHeaderField(sb, "backgroundurl", songMeta.BackgroundUrl);

        AppendHeaderField(sb, "video", songMeta.Video);
        AppendHeaderField(sb, "videourl", songMeta.VideoUrl);
        AppendNumberHeaderField(sb, "videogap", version.IsBefore(UltraStarSongFormatVersion.v200) ? songMeta.TxtFileVideoGapInSeconds : songMeta.VideoGapInMillis);

        AppendNumberHeaderField(sb, "year", songMeta.Year);
        AppendHeaderField(sb, "edition", songMeta.Edition);
        AppendHeaderField(sb, "language", songMeta.Language);
        AppendHeaderField(sb, "genre", songMeta.Genre);
        AppendHeaderField(sb, "tags", songMeta.Tag);

        AppendNumberHeaderField(sb, "start", version.IsBefore(UltraStarSongFormatVersion.v200) ? songMeta.TxtFileStartInSeconds : songMeta.StartInMillis);
        AppendNumberHeaderField(sb, "end", songMeta.EndInMillis);
        AppendNumberHeaderField(sb, "previewstart", version.IsBefore(UltraStarSongFormatVersion.v200) ? songMeta.TxtFilePreviewStartInSeconds : songMeta.PreviewStartInMillis);
        AppendNumberHeaderField(sb, "previewend", version.IsBefore(UltraStarSongFormatVersion.v200) ? songMeta.TxtFilePreviewEndInSeconds : songMeta.PreviewEndInMillis);

        if (version.IsBefore(UltraStarSongFormatVersion.v200))
        {
            AppendNumberHeaderField(sb, "medleystartbeat", songMeta.TxtFileMedleyStartBeat);
            AppendNumberHeaderField(sb, "medleyendbeat", songMeta.TxtFileMedleyEndBeat);
        }
        else
        {
            AppendNumberHeaderField(sb, "medleystart", songMeta.MedleyStartInMillis);
            AppendNumberHeaderField(sb, "medleyend", songMeta.MedleyEndInMillis);
        }

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
