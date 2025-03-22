using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class SongMetaUtils
{
    public static bool SongMetaFileExists(SongMeta songMeta)
    {
        return songMeta?.FileInfo?.Exists ?? false;
    }

    public static bool CoverResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.Cover);
    }

    public static bool BackgroundResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.Background);
    }

    public static bool VideoResourceExists(SongMeta songMeta, Func<string, bool> canHandleUri)
    {
        return ResourceExists(songMeta, GetVideoUriPreferAudioUriIfWebView(songMeta, canHandleUri));
    }

    public static bool AudioResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, GetAudioUri(songMeta));
    }

    public static bool VocalsAudioResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.VocalsAudio);
    }

    public static bool InstrumentalAudioResourceExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.InstrumentalAudio);
    }

    public static string GetCoverUri(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.Cover, songMeta.CoverUrl);
    }

    public static string GetBackgroundUri(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.Background, songMeta.BackgroundUrl);
    }

    public static string GetVideoUri(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.Video, songMeta.VideoUrl);
    }

    public static string GetAudioUri(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.Audio, songMeta.AudioUrl, songMeta.VideoUrl);
    }

    public static string GetVocalsAudioUri(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.VocalsAudio, songMeta.VocalsAudioUrl);
    }

    public static string GetInstrumentalAudioUri(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.InstrumentalAudio, songMeta.InstrumentalAudioUrl);
    }

    public static string GetWebViewUrl(SongMeta songMeta)
    {
        return GetExistingResourceUriOrFirst(songMeta, songMeta.AudioUrl, songMeta.VideoUrl);
    }

    /**
     * When given a file path, checks if the file exists.
     * When given a URI, assumes that the resource exists (always returns true for http and https URIs).
     */
    public static bool ResourceExists(SongMeta songMeta, string pathOrUri)
    {
        if (songMeta == null
            || pathOrUri.IsNullOrEmpty())
        {
            return false;
        }

        if (WebRequestUtils.IsHttpOrHttpsUri(pathOrUri))
        {
            return true;
        }

        if (WebRequestUtils.IsFileUri(pathOrUri))
        {
            return File.Exists(new Uri(pathOrUri).LocalPath);
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

    private static string GetExistingResourceUriOrFirst(SongMeta songMeta, params string[] pathOrUris)
    {
        foreach (string pathOrUri in pathOrUris)
        {
            if (!pathOrUri.IsNullOrEmpty()
                && ResourceExists(songMeta, pathOrUri))
            {
                return GetUri(songMeta, pathOrUri);
            }
        }

        return pathOrUris.FirstOrDefault();
    }

    public static string GetAbsoluteFilePath(SongMeta songMeta, string pathOrUri)
    {
        if (songMeta == null)
        {
            return "";
        }
        return PathUtils.GetAbsoluteFilePath(GetDirectoryPath(songMeta), pathOrUri);
    }

    public static DirectoryInfo GetDirectoryInfo(SongMeta songMeta)
    {
        return songMeta?.FileInfo?.Directory;
    }

    public static string GetDirectoryPath(SongMeta songMeta)
    {
        return songMeta?.FileInfo?.Directory?.FullName ?? "";
    }

    public static string GetAbsoluteSongMetaFilePath(SongMeta songMeta)
    {
        return songMeta?.FileInfo?.FullName ?? "";
    }

    public static List<Sentence> GetSentencesAtBeat(SongMeta songMeta, int beat, bool inclusiveMinBeat = true, bool inclusiveMaxBeat = true)
    {
        return songMeta.Voices
            .SelectMany(voice => voice.Sentences)
            .Where(sentence => IsBeatInSentence(sentence, beat, inclusiveMinBeat, inclusiveMaxBeat))
            .ToList();
    }

    public static Sentence GetSentenceAtBeat(Voice voice, int beat, bool inclusiveMinBeat = true, bool inclusiveMaxBeat = true)
    {
        if (voice == null)
        {
            return null;
        }
        return GetSentenceAtBeat(voice.Sentences, beat, inclusiveMinBeat, inclusiveMaxBeat);
    }

    public static Sentence GetSentenceAtBeat(IReadOnlyCollection<Sentence> sentences, int beat, bool inclusiveMinBeat = true, bool inclusiveMaxBeat = true)
    {
        if (sentences.IsNullOrEmpty())
        {
            return null;
        }
        return sentences.FirstOrDefault(sentence => IsBeatInSentence(sentence, beat, inclusiveMinBeat, inclusiveMaxBeat));
    }

    public static Note GetNoteAtBeat(IEnumerable<Note> notes, int beat, bool inclusiveStartBeat = true, bool inclusiveEndBeat = true)
    {
        if (notes == null)
        {
            return null;
        }

        return notes.FirstOrDefault(note => IsBeatInNote(note, beat, inclusiveStartBeat, inclusiveEndBeat));
    }

    public static Note GetNoteAtBeat(Sentence sentence, int beat, bool inclusiveStartBeat = true, bool inclusiveEndBeat = true)
    {
        if (sentence == null)
        {
            return null;
        }

        return GetNoteAtBeat(sentence.Notes, beat, inclusiveStartBeat, inclusiveEndBeat);
    }

    public static bool IsBeatInNote(Note note, int beat, bool inclusiveStartBeat = true, bool inclusiveEndBeat = true)
    {
        return (note.StartBeat < beat || inclusiveStartBeat && note.StartBeat == beat)
               && (beat < note.EndBeat || inclusiveEndBeat && note.EndBeat == beat);
    }

    public static bool IsBeatInSentence(Sentence sentence, int beat, bool inclusiveMinBeat = true, bool inclusiveMaxBeat = true)
    {
        return (sentence.MinBeat < beat || inclusiveMinBeat && sentence.MinBeat == beat)
               && (beat < sentence.ExtendedMaxBeat || inclusiveMaxBeat && beat == sentence.ExtendedMaxBeat);
    }

    public static Sentence FindExistingSentenceForNote(IEnumerable<Sentence> sentences, Note note)
    {
        return sentences.FirstOrDefault(sentence => sentence.ContainsBeatRange(note.StartBeat, note.EndBeat));
    }

    public static Voice GetOrCreateVoice(SongMeta songMeta, EVoiceId voiceId)
    {
        if (songMeta == null)
        {
            return null;
        }

        if (songMeta.TryGetVoice(voiceId, out Voice existingVoice))
        {
            return existingVoice;
        }

        Voice newVoice = new(voiceId);
        songMeta.AddVoice(newVoice);
        return newVoice;
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
        List<Sentence> sentencesInVoices = songMeta.Voices.SelectMany(voice => voice.Sentences).ToList();
        result.AddRange(sentencesInVoices);
        return result;
    }

    public static List<Note> GetSortedNotes(Sentence sentence)
    {
        List<Note> result = new(sentence.Notes);
        result.Sort(Note.comparerByStartBeat);
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
        DirectoryInfo directoryInfo = GetDirectoryInfo(songMeta);
        if (directoryInfo == null
            || !directoryInfo.Exists)
        {
            return;
        }
        ApplicationUtils.OpenDirectory(directoryInfo.FullName);
    }

    public static string GetLyrics(SongMeta songMeta, EVoiceId voiceId, bool removeTilde = false)
    {
        if (!songMeta.TryGetVoice(voiceId, out Voice voice))
        {
            return "";
        }

        return GetLyrics(voice, removeTilde);
    }

    public static string GetLyrics(List<Note> notes, bool removeTilde = false)
    {
        StringBuilder sb = new();
        notes.ForEach(note =>
        {
            sb.Append(note.Text);
        });
        string lyrics = sb.ToString();
        if (removeTilde)
        {
            lyrics = lyrics.Replace("~", "");
        }

        return lyrics;
    }

    public static string GetLyrics(Voice voice, bool removeTilde = false)
    {
        StringBuilder sb = new();
        voice.Sentences.ForEach(sentence =>
        {
            sb.Append(GetLyrics(sentence));
            sb.Append("\n");
        });
        string lyrics = sb.ToString();
        if (removeTilde)
        {
            lyrics = lyrics.Replace("~", "");
        }

        return lyrics;
    }

    public static string GetLyrics(Sentence sentence)
    {
        StringBuilder sb = new();
        sentence.Notes.ForEach(note =>
        {
            sb.Append(note.Text);
        });
        return sb.ToString();
    }

    public static string GetArtistAndTitle(SongMeta songMeta, string joinWith)
    {
        if (songMeta == null)
        {
            return "";
        }
        return GetArtistAndTitle(songMeta.Artist, songMeta.Title, joinWith);
    }

    private static string GetArtistAndTitle(string artist, string title, string joinWith)
    {
        if (artist.IsNullOrEmpty()
            && title.IsNullOrEmpty())
        {
            return "";
        }

        if (artist.IsNullOrEmpty())
        {
            return title;
        }

        if (title.IsNullOrEmpty())
        {
            return artist;
        }

        return $"{artist}{joinWith}{title}";
    }

    public static int GetMinMidiNote(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return 0;
        }
        return notes.Select(note => note.MidiNote).Min();
    }

    public static int GetMaxMidiNote(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return 0;
        }
        return notes.Select(note => note.MidiNote).Max();
    }

    public static int GetMinBeat(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return 0;
        }
        return notes.Select(note => note.StartBeat).Min();
    }

    public static int GetMaxBeat(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return 0;
        }
        return notes.Select(note => note.EndBeat).Max();
    }

    public static int GetLengthInBeats(List<Note> notes)
    {
        return GetMaxBeat(notes) - GetMinBeat(notes);
    }

    public static double GetNoteDistanceInMillis(SongMeta songMeta, Note noteA, Note noteB)
    {
        int noteDistanceInBeats = Math.Min(
            Math.Abs(noteA.EndBeat - noteB.StartBeat),
            Math.Abs(noteB.EndBeat - noteA.StartBeat));

        return noteDistanceInBeats * SongMetaBpmUtils.MillisPerBeat(songMeta);
    }

    public static string GetVideoUriPreferAudioUriIfWebView(SongMeta songMeta, Func<string, bool> canHandleUri)
    {
        if (songMeta == null)
        {
            return "";
        }

        string audioUri = GetAudioUri(songMeta);
        string videoUri = WebRequestUtils.IsHttpOrHttpsUri(audioUri) && canHandleUri.Invoke(audioUri)
            ? GetAudioUri(songMeta)
            : GetVideoUri(songMeta);
        return videoUri;
    }



    public static bool HasSingAlongData(SongMeta songMeta)
    {
        return !GetAllNotes(songMeta).IsNullOrEmpty();
    }

    public static bool TryGetDistanceInMillis(SongMeta songMeta, Note a, Note b, out double distanceInMillis)
    {
        if (songMeta == null
            || a == null
            || b == null)
        {
            distanceInMillis = 0;
            return false;
        }

        int distanceInBeats = Math.Abs(a.StartBeat - b.EndBeat);
        distanceInMillis = SongMetaBpmUtils.BeatsToMillisWithoutGap(songMeta, distanceInBeats);
        return true;
    }

    public static Voice GetVoiceById(SongMeta songMeta, EVoiceId voiceId)
    {
        if (songMeta == null)
        {
            return null;
        }

        if (songMeta.TryGetVoice(voiceId, out Voice voice))
        {
            return voice;
        }

        return null;
    }

    public static Dictionary<EVoiceId, string> GetVoiceIdToDisplayName(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return new Dictionary<EVoiceId, string>();
        }

        Dictionary<EVoiceId, string> result = new();
        foreach (EVoiceId voiceId in EnumUtils.GetValuesAsList<EVoiceId>())
        {
            string displayName = songMeta.GetVoiceDisplayName(voiceId);
            result[voiceId] = displayName;
        }
        return result;
    }
}
