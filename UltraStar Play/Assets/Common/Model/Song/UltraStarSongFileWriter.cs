using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class UltraStarSongFileWriter
{
    public static void WriteFile(string absolutePath, SongMeta songMeta)
    {
        string txtFileContent = GetTxtFileContent(songMeta);
        File.WriteAllText(absolutePath, txtFileContent, Encoding.UTF8);
    }

    private static string GetTxtFileContent(SongMeta songMeta)
    {
        StringBuilder sb = new StringBuilder();
        AppendHeader(sb, songMeta);
        List<Voice> sortedVoices = new List<Voice>(songMeta.GetVoices().Values);
        sortedVoices.Sort(Voice.comparerByName);
        foreach (Voice voice in sortedVoices)
        {
            string voiceName = voice.Name;
            if (voiceName.IsNullOrEmpty() && sortedVoices.Count > 1)
            {
                voiceName = "P1";
            }
            if (!voiceName.IsNullOrEmpty() && sortedVoices.Count == 1)
            {
                voiceName = "";
            }

            AppendVoice(sb, voice, voiceName);
        }
        sb.Append("E");
        return sb.ToString();
    }

    private static void AppendVoice(StringBuilder sb, Voice voice, string voiceName)
    {
        bool isEmpty = voice.Sentences.SelectMany(it => it.Notes).Any();
        if (!isEmpty)
        {
            return;
        }
        if (!voiceName.IsNullOrEmpty())
        {
            sb.AppendLine(voiceName);
        }
        foreach (Sentence sentence in voice.Sentences)
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

        foreach (Note note in sentence.Notes)
        {
            AppendNote(sb, note);
        }
        sb.AppendLine($"- {sentence.ExtendedMaxBeat}");
    }

    private static void AppendNote(StringBuilder sb, Note note)
    {
        sb.AppendLine($"{GetNotePrefix(note)} {note.StartBeat} {note.Length} {note.TxtPitch} {note.Text}");
    }

    private static string GetNotePrefix(Note note)
    {
        if (note.IsGolden)
        {
            return "*";
        }
        if (note.IsFreestyle)
        {
            return "F";
        }
        return ":";
    }

    private static void AppendHeader(StringBuilder sb, SongMeta songMeta)
    {
        AppendHeaderField(sb, "encoding", "UTF8");

        AppendHeaderField(sb, "title", songMeta.Title);
        AppendHeaderField(sb, "artist", songMeta.Artist);
        AppendHeaderField(sb, "mp3", songMeta.Mp3);
        AppendHeaderField(sb, "bpm", songMeta.Bpm.ToString());
        if (songMeta.Gap > 0)
        {
            AppendHeaderField(sb, "gap", songMeta.Gap.ToString());
        }

        AppendHeaderField(sb, "cover", songMeta.Cover);
        AppendHeaderField(sb, "background", songMeta.Background);

        AppendHeaderField(sb, "video", songMeta.Video);
        if (songMeta.VideoGap > 0)
        {
            AppendHeaderField(sb, "videogap", songMeta.VideoGap.ToString());
        }

        AppendHeaderField(sb, "genre", songMeta.Genre);
        if (songMeta.Year > 0)
        {
            AppendHeaderField(sb, "year", songMeta.Year.ToString());
        }

        AppendHeaderField(sb, "language", songMeta.Language);
        AppendHeaderField(sb, "edition", songMeta.Edition);

        if (songMeta.Start > 0)
        {
            AppendHeaderField(sb, "start", songMeta.Start.ToString());
        }
        if (songMeta.End > 0)
        {
            AppendHeaderField(sb, "end", songMeta.End.ToString());
        }

        foreach (KeyValuePair<string, string> unkownHeaderEntry in songMeta.UnkownHeaderEntries)
        {
            AppendHeaderField(sb, unkownHeaderEntry.Key, unkownHeaderEntry.Value);
        }
    }

    private static void AppendHeaderField(StringBuilder sb, string key, string value)
    {
        if (!value.IsNullOrEmpty())
        {
            sb.AppendLine($"#{key.ToUpper()}:{value}");
        }
    }
}