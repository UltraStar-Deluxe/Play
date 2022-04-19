using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class SongMetaUtils
{
    public static string GetCoverUri(SongMeta songMeta)
    {
        return GetUri(songMeta, songMeta.Cover);
    }

    public static string GetBackgroundUri(SongMeta songMeta)
    {
        return GetUri(songMeta, songMeta.Background);
    }

    public static string GetVideoUri(SongMeta songMeta)
    {
        return GetUri(songMeta, songMeta.Video);
    }

    public static string GetAudioUri(SongMeta songMeta)
    {
        return GetUri(songMeta, songMeta.Mp3);
    }

    private static string GetUri(SongMeta songMeta, string pathOrUri)
    {
        if (pathOrUri.IsNullOrEmpty())
        {
            return "";
        }

        if (WebRequestUtils.IsHttpOrHttpsUri(pathOrUri))
        {
            return pathOrUri;
        }

        return "file://" + songMeta.Directory + Path.DirectorySeparatorChar + pathOrUri;
    }

    public static string GetAbsoluteSongMetaPath(SongMeta songMeta)
    {
        return songMeta.Directory + Path.DirectorySeparatorChar + songMeta.Filename;
    }

    public static List<Sentence> GetSentencesAtBeat(SongMeta songMeta, int beat)
    {
        return songMeta.GetVoices().SelectMany(voice => voice.Sentences)
            .Where(sentence => IsBeatInSentence(sentence, beat)).ToList();
    }

    public static Sentence GetSentenceAtBeat(Voice voice, int beat)
    {
        if (voice == null)
        {
            return null;
        }
        return voice.Sentences.FirstOrDefault(sentence => sentence.MinBeat <= beat && beat <= sentence.MaxBeat);
    }

    public static Note GetNoteAtBeat(Sentence sentence, int beat)
    {
        if (sentence == null)
        {
            return null;
        }
        return sentence.Notes.FirstOrDefault(note => note.StartBeat <= beat && beat <= note.EndBeat);
    }

    public static bool IsBeatInSentence(Sentence sentence, int beat)
    {
        return sentence.MinBeat <= beat && beat <= sentence.ExtendedMaxBeat;
    }

    public static Sentence FindExistingSentenceForNote(IEnumerable<Sentence> sentences, Note note)
    {
        return sentences.FirstOrDefault(sentence => sentence.ContainsBeatRange(note.StartBeat, note.EndBeat));
    }

    public static Voice GetOrCreateVoice(SongMeta songMeta, string voiceName)
    {
        Voice matchingVoice = songMeta.GetVoices()
            .FirstOrDefault(voice => voice.VoiceNameEquals(voiceName));
        if (matchingVoice != null)
        {
            return matchingVoice;
        }

        // Create new voice.
        // Set voice identifier for solo voice because this is not a solo song anymore.
        Voice soloVoice = songMeta.GetVoices().FirstOrDefault(it => it.Name == Voice.soloVoiceName);
        if (soloVoice != null)
        {
            soloVoice.SetName(Voice.firstVoiceName);
        }

        Voice newVoice = new(voiceName);
        songMeta.AddVoice(newVoice);

        return newVoice;
    }

    public static List<Note> GetFollowingNotes(SongMeta songMeta, List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        int maxBeat = notes.Select(it => it.EndBeat).Max();
        List<Note> result = GetAllSentences(songMeta)
            .SelectMany(sentence => sentence.Notes)
            .Where(note => note.StartBeat >= maxBeat)
            .ToList();
        return result;
    }

    // Returns the notes in the song as well as the notes in the layers in no particular order.
    public static List<Note> GetAllNotes(SongMeta songMeta)
    {
        List<Note> result = GetAllSentences(songMeta).SelectMany(sentence => sentence.Notes).ToList();
        return result;
    }

    public static List<Note> GetAllNotes(Voice voice)
    {
        List<Note> result = voice.Sentences.SelectMany(sentence => sentence.Notes).ToList();
        return result;
    }

    public static List<Sentence> GetAllSentences(SongMeta songMeta)
    {
        List<Sentence> result = new();
        List<Sentence> sentencesInVoices = songMeta.GetVoices().SelectMany(voice => voice.Sentences).ToList();
        result.AddRange(sentencesInVoices);
        return result;
    }

    public static Sentence GetNextSentence(Sentence sentence)
    {
        if (sentence.Voice == null)
        {
            return null;
        }

        List<Sentence> sortedSentencesOfVoice = new(sentence.Voice.Sentences);
        sortedSentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sortedSentencesOfVoice)
        {
            if (lastSentence == sentence)
            {
                return s;
            }
            lastSentence = s;
        }
        return null;
    }

    public static Sentence GetPreviousSentence(Sentence sentence)
    {
        if (sentence.Voice == null)
        {
            return null;
        }

        List<Sentence> sortedSentencesOfVoice = new(sentence.Voice.Sentences);
        sortedSentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sortedSentencesOfVoice)
        {
            if (s == sentence)
            {
                return lastSentence;
            }
            lastSentence = s;
        }
        return null;
    }

    public static List<Note> GetSortedNotes(Sentence sentence)
    {
        List<Note> result = new(sentence.Notes);
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    public static List<Note> GetSortedNotes(SongMeta songMeta)
    {
        List<Note> result = GetAllNotes(songMeta);
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    public static List<Sentence> GetSortedSentences(SongMeta songMeta)
    {
        List<Sentence> result = GetAllSentences(songMeta);
        result.Sort(Sentence.comparerByStartBeat);
        return result;
    }

    public static List<Sentence> GetSortedSentences(Voice voice)
    {
        List<Sentence> result = new(voice.Sentences);
        result.Sort(Sentence.comparerByStartBeat);
        return result;
    }

    public static void OpenDirectory(SongMeta songMeta)
    {
        if (songMeta == null || !Directory.Exists(songMeta.Directory))
        {
            return;
        }

        Application.OpenURL("file://" + songMeta.Directory);
    }

    public static string GetLyrics(SongMeta songMeta, string voiceName)
    {
        Voice voice = songMeta.GetVoices().FirstOrDefault(voice => voice.VoiceNameEquals(voiceName));
        if (voice == null)
        {
            return "";
        }

        return GetLyrics(songMeta, voice);
    }

    public static string GetLyrics(SongMeta songMeta, Voice voice)
    {
        StringBuilder sb = new();
        voice.Sentences.ForEach(sentence =>
        {
            sentence.Notes.ForEach(note =>
            {
                sb.Append(note.Text);
            });
            sb.Append("\n");
        });
        return sb.ToString();
    }
}
