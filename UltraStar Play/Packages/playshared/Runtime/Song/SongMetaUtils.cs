using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class SongMetaUtils
{
    public static bool CoverResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.Cover);
    }

    public static bool BackgroundResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.Background);
    }

    public static bool VideoResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.Video);
    }

    public static bool AudioResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.Mp3);
    }

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

    /**
     * Checks if a file exists.
     * Assumes that the resource behind a http and https URI exists (always returns true for these URIs).
     */
    private static bool ResourceExists(SongMeta songMeta, string pathOrUri)
    {
        if (WebRequestUtils.IsHttpOrHttpsUri(pathOrUri))
        {
            return true;
        }

        return File.Exists(GetAbsoluteFilePath(songMeta, pathOrUri));
    }

    /**
     * Returns the URI or absolute file system path to a resource.
     */
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

        // The given path is relative to the song file. Make it absolute.
        string absoluteFilePath = GetAbsoluteFilePath(songMeta, pathOrUri);
        return WebRequestUtils.AbsoluteFilePathToUri(absoluteFilePath);
    }

    private static string GetAbsoluteFilePath(SongMeta songMeta, string path)
    {
        if (PathUtils.IsAbsolutePath(path))
        {
            return path;
        }

        return songMeta.Directory + Path.DirectorySeparatorChar + path;
    }

    public static string GetAbsoluteSongMetaPath(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }
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

    public static Note GetNoteAtBeat(Sentence sentence, int beat, bool inclusiveStartBeat = true, bool inclusiveEndBeat = true)
    {
        if (sentence == null)
        {
            return null;
        }

        return sentence.Notes.FirstOrDefault(note =>
            (note.StartBeat < beat || inclusiveStartBeat && note.StartBeat == beat)
            && (beat < note.EndBeat || inclusiveEndBeat && note.EndBeat == beat));
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
        if (voice == null)
        {
            return new List<Note>();
        }
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

        ApplicationUtils.OpenDirectory(songMeta.Directory);
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

    // Checks whether the audio and video file formats of the song are supported.
    // Returns true iff the audio file of the SongMeta exists and is supported.
    public static List<SongIssue> GetSupportedMediaFormatIssues(SongMeta songMeta)
    {
        List<SongIssue> songIssues = new();

        // Check video format.
        // Video is optional.
        if (!songMeta.Video.IsNullOrEmpty())
        {
            if (!ApplicationUtils.IsSupportedVideoFormat(Path.GetExtension(songMeta.Video)))
            {
                songIssues.Add(SongIssue.CreateWarning(songMeta, $"Unsupported video format {Path.GetExtension(songMeta.Video)}"));
                // Do not attempt to load the video file
                songMeta.Video = "";
            }
            else if (!VideoResourceExists(songMeta))
            {
                songIssues.Add(SongIssue.CreateWarning(songMeta, $"Video file resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(GetVideoUri(songMeta))}'"));
                // Do not attempt to load the video file
                songMeta.Video = "";
            }
        }

        // Check audio format.
        // Audio is mandatory. Without working audio file, the song cannot be played.
        if (!ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(songMeta.Mp3)))
        {
            songIssues.Add(SongIssue.CreateError(songMeta, $"Unsupported audio format {Path.GetExtension(songMeta.Mp3)}"));
        }
        else if (!AudioResourceExists(songMeta))
        {
            songIssues.Add(SongIssue.CreateError(songMeta, $"Audio file resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(GetAudioUri(songMeta))}'"));
        }

        // Log found issues
        songIssues.ForEach(songIssue => songIssue.Log());

        return songIssues;
    }

    public static string GetArtistDashTitle(SongMeta songMeta)
    {
        return $"{songMeta?.Artist} - {songMeta?.Title}";
    }
}
