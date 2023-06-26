using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class SongMetaUtils
{
    private static readonly HashBasedColorGenerator colorGenerator = new(
        o => o?.GetHashCode() ?? 0,
        new Vector2(0.4f, 1f),
        new Vector2(0.7f, 1f));
    
    public static bool SongMetaFileExists(SongMeta songMeta)
    {
        return ResourceExists(songMeta, songMeta.FileName);
    }

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

    public static string GetVocalsAudioUri(SongMeta songMeta)
    {
        return GetUri(songMeta, songMeta.VocalsAudio);
    }

    public static string GetInstrumentalAudioUri(SongMeta songMeta)
    {
        return GetUri(songMeta, songMeta.InstrumentalAudio);
    }

    /**
     * Checks if a file exists.
     * Assumes that the resource behind a http and https URI exists (always returns true for these URIs).
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

    public static string GetAbsoluteFilePath(SongMeta songMeta, string path)
    {
        if (PathUtils.IsAbsolutePath(path))
        {
            return path;
        }

        if (songMeta == null)
        {
            return "";
        }
        return songMeta.Directory + $"/{path}";
    }

    public static bool IsGeneratedAndSaved(SongMeta songMeta)
    {
        if (songMeta == null
            || songMeta.Directory.IsNullOrEmpty()
            || !Directory.Exists(songMeta.Directory))
        {
            return false;
        }

        string songMetaAbsolutePath = new DirectoryInfo(songMeta.Directory).FullName;
        string generatedSongFolderAbsolutePath = new DirectoryInfo(ApplicationUtils.GetGeneratedSongFolderAbsolutePath()).FullName;
        return songMetaAbsolutePath.Contains(generatedSongFolderAbsolutePath)
               && File.Exists(GetAbsoluteSongMetaFilePath(songMeta));
    }

    public static bool IsGeneratedAndNotYetSaved(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return false;
        }

        if (songMeta.Directory.IsNullOrEmpty()
            || !Directory.Exists(songMeta.Directory))
        {
            return true;
        }

        string songMetaAbsolutePath = new DirectoryInfo(songMeta.Directory).FullName;
        string generatedSongFolderAbsolutePath = new DirectoryInfo(ApplicationUtils.GetGeneratedSongFolderAbsolutePath()).FullName;
        return songMetaAbsolutePath.Contains(generatedSongFolderAbsolutePath)
               && !File.Exists(GetAbsoluteSongMetaFilePath(songMeta));
    }

    public static void CreateDirectory(SongMeta songMeta)
    {
        if (!songMeta.Directory.IsNullOrEmpty()
            && !Directory.Exists(songMeta.Directory))
        {
            Directory.CreateDirectory(songMeta.Directory);
        }
    }

    public static string GetAbsoluteSongMetaFilePath(SongMeta songMeta)
    {
        return GetAbsoluteFilePath(songMeta, songMeta.FileName);
    }

    public static List<Sentence> GetSentencesAtBeat(SongMeta songMeta, int beat, bool inclusiveMinBeat = true, bool inclusiveMaxBeat = true)
    {
        return songMeta.GetVoices()
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
        return voice.Sentences.FirstOrDefault(sentence => IsBeatInSentence(sentence, beat, inclusiveMinBeat, inclusiveMaxBeat));
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

    public static Voice GetOrCreateVoice(SongMeta songMeta, string voiceName)
    {
        Voice matchingVoice = songMeta.GetVoices()
            .FirstOrDefault(voice => Voice.VoiceNameEquals(voice.Name, voiceName));
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

    public static string GetLyrics(SongMeta songMeta, string voiceName, bool removeTilde = false)
    {
        Voice voice = songMeta.GetVoices().FirstOrDefault(voice => Voice.VoiceNameEquals(voice.Name, voiceName));
        if (voice == null)
        {
            return "";
        }

        return GetLyrics(voice, removeTilde);
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

    public static string GetArtistDashTitle(SongMeta songMeta)
    {
        return GetArtistDashTitle(songMeta.Artist, songMeta.Title);
    }

    public static string GetArtistDashTitle(string artist, string title)
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

        return $"{artist} - {title}";
    }
    
    public static int MinBeat(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return 0;
        }
        return notes.Select(note => note.StartBeat).Min();
    }

    public static int MaxBeat(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return 0;
        }
        return notes.Select(note => note.EndBeat).Max();
    }

    public static int LengthInBeats(List<Note> notes)
    {
        return MaxBeat(notes) - MinBeat(notes);
    }

    public static void RemoveAllNotes(SongMeta songMeta)
    {
        songMeta.GetVoices().ForEach(voice =>
            voice.Sentences.ToList().ForEach(sentence => voice.RemoveSentence(sentence)));
    }

    public static double NoteDistanceInMillis(SongMeta songMeta, Note noteA, Note noteB)
    {
        int noteDistanceInBeats = Math.Min(
            Math.Abs(noteA.EndBeat - noteB.StartBeat),
            Math.Abs(noteB.EndBeat - noteA.StartBeat));

        return noteDistanceInBeats * BpmUtils.MillisecondsPerBeat(songMeta);
    }

    public static string GetMedleyName(List<SongMeta> songMetas)
    {
        if (songMetas.IsNullOrEmpty())
        {
            return "";
        }

        if (songMetas.Count == 1)
        {
            return GetArtistDashTitle(songMetas[0]);
        }

        return songMetas
            .Select(songMeta => songMeta.Title)
            .JoinWith(", ");
    }

    public static int GetMedleyStartBeat(SongMeta songMeta)
    {
        if (songMeta.MedleyStartBeat >= 0)
        {
            return songMeta.MedleyStartBeat;
        }
        else
        {
            return GetDefaultMedleyStartBeat(songMeta);
        }
    }

    public static int GetMedleyEndBeat(SongMeta songMeta, int targetDurationInSeconds)
    {
        if (songMeta.MedleyEndBeat >= 0)
        {
            return songMeta.MedleyEndBeat;
        }
        else
        {
            return GetDefaultMedleyEndBeat(songMeta, targetDurationInSeconds);
        }
    }

    private static int GetDefaultMedleyStartBeat(SongMeta songMeta)
    {
        // Search for lyrics about the middle of the song, approx. 20 seconds afterwards.
        int middleBeat = GetMiddleBeat(songMeta);
        Voice voice = songMeta.GetVoice(Voice.firstVoiceName);
        List<Sentence> sentences = voice.Sentences.ToList();
        List<Sentence> sentencesBeforeMiddleBeat = sentences
            .Where(sentence => sentence.ExtendedMaxBeat < middleBeat)
            .ToList();
        if (sentencesBeforeMiddleBeat.IsNullOrEmpty())
        {
            // Should not happen, this is a weird song.
            Debug.LogWarning("Could not calculate a nice medley start beat. Using the middle of the song instead.");
            return middleBeat;
        }

        sentencesBeforeMiddleBeat.Sort(Sentence.comparerByStartBeat);
        return sentencesBeforeMiddleBeat.LastOrDefault().MinBeat;
    }

    private static int GetDefaultMedleyEndBeat(SongMeta songMeta, int targetDurationInSeconds)
    {
        // End the medley approx. 30 seconds afterward the start.
        int medleyStartBeta = GetMedleyStartBeat(songMeta);
        int targetDurationInBeats = (int)BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, targetDurationInSeconds * 1000);
        int targetEndBeat = medleyStartBeta + targetDurationInBeats;

        List<Sentence> sentencesAfterMedleyStart = songMeta.GetVoice(Voice.firstVoiceName)
            .Sentences
            .Where(sentence => sentence.MinBeat > medleyStartBeta)
            .ToList();

        if (sentencesAfterMedleyStart.IsNullOrEmpty())
        {
            // Should not happen, this is a weird song.
            Debug.LogWarning("Could not calculate a nice medley end beat. Using some beats after medley start instead.");
            return medleyStartBeta + targetDurationInBeats;
        }

        Sentence sentence = sentencesAfterMedleyStart.FindMinElement(sentence =>
        {
            // Use sentence which best approximates the target distance.
            float distanceToTargetBeat = Math.Abs(sentence.ExtendedMaxBeat - targetEndBeat);
            return distanceToTargetBeat;
        });
        if (sentence == null)
        {
            return medleyStartBeta + 1;
        }
        return sentence.ExtendedMaxBeat;
    }

    private static int GetMiddleBeat(SongMeta songMeta)
    {
        // Search for lyrics about the middle of the song, approx. 20 seconds afterwards.
        List<Note> allNotes = GetAllNotes(songMeta);
        int minBeat = MinBeat(allNotes);
        int maxBeat = MaxBeat(allNotes);
        return minBeat + ((maxBeat - minBeat) / 2);
    }

    public static string GetRelativePath(SongMeta songMeta, string path)
    {
        string relativePath = PathUtils.MakeRelativePath(songMeta.Directory, path);
        return relativePath;
    }
    
    public static string GetAttributionText(SongMeta selectedSong)
    {
        string GetAttributionText(string title, string author, string license, string source)
        {
            List<string> parts = new List<string>();
            if (!author.IsNullOrEmpty())
            {
                parts.Add(author);
            }
            if (!license.IsNullOrEmpty())
            {
                parts.Add($"License: {license}");
            }
            if (!source.IsNullOrEmpty())
            {
                parts.Add($"Source: {source}");
            }
            
            if (parts.IsNullOrEmpty())
            {
                return "";
            }

            return parts.ToCsv("\n   ", $"• {title}: ", "");
        }
        
        string audioAuthor = selectedSong.GetUnknownHeaderEntry($"AUDIOAUTHOR");
        if (audioAuthor.IsNullOrEmpty())
        {
            audioAuthor = selectedSong.Artist;
        }
        string audioLicense = selectedSong.GetUnknownHeaderEntry($"AUDIOLICENSE");
        string audioSource = selectedSong.GetUnknownHeaderEntry($"AUDIOSOURCE");
        
        string lyricsAuthor = selectedSong.GetUnknownHeaderEntry($"LYRICSAUTHOR");
        string lyricsLicense = selectedSong.GetUnknownHeaderEntry($"LYRICSLICENSE");
        string lyricsSource = selectedSong.GetUnknownHeaderEntry($"LYRICSSOURCE");
        
        string backgroundAuthor = selectedSong.GetUnknownHeaderEntry($"BACKGROUNDAUTHOR");
        string backgroundLicense = selectedSong.GetUnknownHeaderEntry($"BACKGROUNDLICENSE");
        string backgroundSource = selectedSong.GetUnknownHeaderEntry($"BACKGROUNDSOURCE");
        
        string coverAuthor = selectedSong.GetUnknownHeaderEntry($"COVERAUTHOR");
        string coverLicense = selectedSong.GetUnknownHeaderEntry($"COVERLICENSE");
        string coverSource = selectedSong.GetUnknownHeaderEntry($"COVERSOURCE");
        
        string videoAuthor = selectedSong.GetUnknownHeaderEntry($"VIDEOAUTHOR");
        string videoLicense = selectedSong.GetUnknownHeaderEntry($"VIDEOLICENSE");
        string videoSource = selectedSong.GetUnknownHeaderEntry($"VIDEOSOURCE");
        
        return new List<string>()
        {
            GetAttributionText("Audio", audioAuthor, audioLicense, audioSource),
            GetAttributionText("Lyrics", lyricsAuthor, lyricsLicense, lyricsSource),
            GetAttributionText("Video", videoAuthor, videoLicense, videoSource),
            GetAttributionText("Background", backgroundAuthor, backgroundLicense, backgroundSource),
            GetAttributionText("Cover", coverAuthor, coverLicense, coverSource),
        }.Where(it => !it.IsNullOrEmpty()).ToCsv("\n", "", "");
    }

    public static Voice CreateMergedVoice(List<Voice> voices)
    {
        if (voices.Count <= 1)
        {
            return voices.FirstOrDefault();
        }
        
        Voice mergedVoice = new();
        foreach (Voice voice in voices.ToList())
        {
            foreach (Sentence newSentence in voice.Sentences.ToList())
            {
                // Add the sentence if there is none yet.
                Sentence overlappingSentence = mergedVoice.Sentences
                    .FirstOrDefault(existingSentence => IsBeatInSentence(existingSentence, newSentence.MinBeat, true, false) 
                                                        || IsBeatInSentence(existingSentence, newSentence.MaxBeat, true, false));
                if (overlappingSentence != null)
                {
                    Debug.Log($"{newSentence} overlaps with {overlappingSentence}");
                }
                
                if (overlappingSentence == null)
                {
                    Sentence newSentenceClone = newSentence.CloneDeep();
                    mergedVoice.AddSentence(newSentenceClone);
                }
            }
        }
        
        // Minimize sentences to make sure that they do not overlap
        foreach (Sentence mergedSentence in mergedVoice.Sentences)
        {
            mergedSentence.SetLinebreakBeat(0);
        }

        // Sort sentences
        List<Sentence> sortedSentences = mergedVoice.Sentences.ToList();
        sortedSentences.Sort(Sentence.comparerByStartBeat);
        mergedVoice.SetSentences(sortedSentences);
        
        return mergedVoice;
    }

    public static void AddTrailingSpaceToLastNoteOfSentence(Sentence sentence)
    {
        if (sentence == null)
        {
            return;
        }
        
        AddTrailingSpaceToLastNoteOfSentence(sentence.Notes.LastOrDefault());
    }

    public static void AddTrailingSpaceToLastNoteOfSentence(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return;
        }
        
        notes.ForEach(note => AddTrailingSpaceToLastNoteOfSentence(note));
    }
    
    public static void AddTrailingSpaceToLastNoteOfSentence(Note note)
    {
        if (note == null)
        {
            return;
        }
        
        // Add space at end of note if it was the last note in the sentence. Otherwise, formerly separate words might be merged.
        if (!note.Text.EndsWith(" ")
            && note.Sentence != null
            && note.Sentence.Notes.LastOrDefault() == note)
        {
            note.SetText(note.Text + " ");
        }
    }

    public static string GetVideoUriPreferAudioUriIfWebView(SongMeta songMeta, Func<string, bool> canHandleUri)
    {
        if (songMeta == null)
        {
            return "";
        }
        
        string videoUri = WebRequestUtils.IsHttpOrHttpsUri(songMeta.Mp3) && canHandleUri.Invoke(songMeta.Mp3)
            ? SongMetaUtils.GetAudioUri(songMeta)
            : SongMetaUtils.GetVideoUri(songMeta);
        return videoUri;
    }
    
    public static Color32 CreateColorForSongMeta(SongMeta songMeta)
    {
        string artistDashTitle = GetArtistDashTitle(songMeta);
        if (artistDashTitle.IsNullOrEmpty())
        {
            return Color.white;
        }
        
        return colorGenerator.ToColor(artistDashTitle);
    }

    public static string GetScoreRelevantSongHash(SongMeta songMeta)
    {
        StringBuilder sb = new();
        sb.Append("{");
        
        sb.Append("BPM:");
        sb.Append(songMeta.Bpm.ToStringInvariantCulture());
        
        int voiceIndex = 1;
        foreach (Voice voice in songMeta.GetVoices())
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
                sb.Append(UltraStarSongFileWriter.GetNoteTypePrefix(note.Type));
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
        Log.Verbose(() => $"{songMeta} has ScoreRelevantSongHash {scoreRelevantSongHash}, from string: {sb}");
        return scoreRelevantSongHash;
    }
}
