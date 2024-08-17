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

    public static bool IsGeneratedAndSaved(SongMeta songMeta, string generatedSongFolderAbsolutePath)
    {
        DirectoryInfo directoryInfo = GetDirectoryInfo(songMeta);
        if (directoryInfo == null
            || !directoryInfo.Exists)
        {
            return false;
        }

        string songMetaAbsolutePath = directoryInfo.FullName;
        return songMetaAbsolutePath.Contains(generatedSongFolderAbsolutePath)
               && File.Exists(GetAbsoluteSongMetaFilePath(songMeta));
    }

    public static DirectoryInfo GetDirectoryInfo(SongMeta songMeta)
    {
        return songMeta?.FileInfo?.Directory;
    }

    public static string GetDirectoryPath(SongMeta songMeta)
    {
        return songMeta?.FileInfo?.Directory?.FullName ?? "";
    }

    public static void CreateDirectory(SongMeta songMeta)
    {
        DirectoryInfo directoryInfo = GetDirectoryInfo(songMeta);
        if (directoryInfo == null
            || directoryInfo.Exists)
        {
            return;
        }

        directoryInfo.Create();
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
        List<Sentence> sentencesInVoices = songMeta.Voices.SelectMany(voice => voice.Sentences).ToList();
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

    public static string GetArtistDashTitle(SongMeta songMeta)
    {
        return GetArtistAndTitle(songMeta, " - ");
    }

    public static string GetArtistAndTitle(SongMeta songMeta, string joinWith)
    {
        if (songMeta == null)
        {
            return "";
        }
        return GetArtistAndTitle(songMeta.Artist, songMeta.Title, joinWith);
    }

    public static string GetArtistAndTitle(string artist, string title, string joinWith)
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
        songMeta.Voices.ForEach(voice =>
            voice.Sentences.ToList().ForEach(sentence => voice.RemoveSentence(sentence)));
    }

    public static double NoteDistanceInMillis(SongMeta songMeta, Note noteA, Note noteB)
    {
        int noteDistanceInBeats = Math.Min(
            Math.Abs(noteA.EndBeat - noteB.StartBeat),
            Math.Abs(noteB.EndBeat - noteA.StartBeat));

        return noteDistanceInBeats * SongMetaBpmUtils.MillisPerBeat(songMeta);
    }

    public static string GetMedleyName(List<SongMeta> songMetas)
    {
        if (songMetas.IsNullOrEmpty())
        {
            return "";
        }

        if (songMetas.Count == 1)
        {
            songMetas[0].GetArtistDashTitle();
        }

        return songMetas
            .Select(songMeta => songMeta.Title)
            .JoinWith(", ");
    }

    public static int GetMedleyStartBeat(SongMeta songMeta)
    {
        if (songMeta.MedleyStartInMillis > 0)
        {
            return (int)SongMetaBpmUtils.MillisToBeats(songMeta, songMeta.MedleyStartInMillis);
        }
        else
        {
            return GetDefaultMedleyStartBeat(songMeta);
        }
    }

    public static int GetMedleyEndBeat(SongMeta songMeta, int targetDurationInSeconds)
    {
        if (songMeta.MedleyEndInMillis > 0)
        {
            return (int)SongMetaBpmUtils.MillisToBeats(songMeta, songMeta.MedleyEndInMillis);
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
        Voice voice = GetVoiceById(songMeta, EVoiceId.P1);
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
        int targetDurationInBeats = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, targetDurationInSeconds * 1000);
        int targetEndBeat = medleyStartBeta + targetDurationInBeats;

        List<Sentence> sentencesAfterMedleyStart = GetVoiceById(songMeta, EVoiceId.P1)
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
        string directoryPath = GetDirectoryPath(songMeta);
        if (directoryPath.IsNullOrEmpty())
        {
            return path;
        }

        string relativePath = PathUtils.MakeRelativePath(directoryPath, path);
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

            return parts.JoinWith("\n   ", $"• {title}: ", "");
        }

        string audioAuthor = selectedSong.GetAdditionalHeaderEntry($"AUDIOAUTHOR");
        if (audioAuthor.IsNullOrEmpty())
        {
            audioAuthor = selectedSong.Artist;
        }
        string audioLicense = selectedSong.GetAdditionalHeaderEntry($"AUDIOLICENSE");
        string audioSource = selectedSong.GetAdditionalHeaderEntry($"AUDIOSOURCE");

        string lyricsAuthor = selectedSong.GetAdditionalHeaderEntry($"LYRICSAUTHOR");
        string lyricsLicense = selectedSong.GetAdditionalHeaderEntry($"LYRICSLICENSE");
        string lyricsSource = selectedSong.GetAdditionalHeaderEntry($"LYRICSSOURCE");

        string backgroundAuthor = selectedSong.GetAdditionalHeaderEntry($"BACKGROUNDAUTHOR");
        string backgroundLicense = selectedSong.GetAdditionalHeaderEntry($"BACKGROUNDLICENSE");
        string backgroundSource = selectedSong.GetAdditionalHeaderEntry($"BACKGROUNDSOURCE");

        string coverAuthor = selectedSong.GetAdditionalHeaderEntry($"COVERAUTHOR");
        string coverLicense = selectedSong.GetAdditionalHeaderEntry($"COVERLICENSE");
        string coverSource = selectedSong.GetAdditionalHeaderEntry($"COVERSOURCE");

        string videoAuthor = selectedSong.GetAdditionalHeaderEntry($"VIDEOAUTHOR");
        string videoLicense = selectedSong.GetAdditionalHeaderEntry($"VIDEOLICENSE");
        string videoSource = selectedSong.GetAdditionalHeaderEntry($"VIDEOSOURCE");

        return new List<string>()
        {
            GetAttributionText("Audio", audioAuthor, audioLicense, audioSource),
            GetAttributionText("Lyrics", lyricsAuthor, lyricsLicense, lyricsSource),
            GetAttributionText("Video", videoAuthor, videoLicense, videoSource),
            GetAttributionText("Background", backgroundAuthor, backgroundLicense, backgroundSource),
            GetAttributionText("Cover", coverAuthor, coverLicense, coverSource),
        }.Where(it => !it.IsNullOrEmpty()).JoinWith("\n");
    }

    public static Voice CreateMergedVoice(List<Voice> voices)
    {
        if (voices.IsNullOrEmpty())
        {
            return null;
        }

        if (voices.Count == 1)
        {
            return voices.FirstOrDefault();
        }

        MergedVoice mergedVoice = new(voices);
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

        string audioUri = GetAudioUri(songMeta);
        string videoUri = WebRequestUtils.IsHttpOrHttpsUri(audioUri) && canHandleUri.Invoke(audioUri)
            ? GetAudioUri(songMeta)
            : GetVideoUri(songMeta);
        return videoUri;
    }

    public static Color32 CreateColorForSongMeta(SongMeta songMeta)
    {
        return ColorGenerationUtils.FromString(songMeta.GetArtistDashTitle());
    }

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

    public static void AddVoice(SongMeta songMeta, Voice voice)
    {
        if (songMeta == null
            || voice == null)
        {
            return;
        }

        songMeta.AddVoice(voice);
    }

    public static void RemoveVoice(SongMeta songMeta, EVoiceId voiceId)
    {
        if (songMeta == null)
        {
            return;
        }

        songMeta.RemoveVoice(voiceId);
    }

    public static void RemoveVoice(SongMeta songMeta, Voice voice)
    {
        if (songMeta == null
            || voice == null)
        {
            return;
        }

        RemoveVoice(songMeta, voice.Id);
    }

    public static bool HasFailedToLoadVoices(SongMeta songMeta)
    {
        return songMeta is LazyLoadedVoicesSongMeta lazyLoadedVoicesSongMeta
               && lazyLoadedVoicesSongMeta.LoadVoicesPhase is LazyLoadedVoicesSongMeta.ELoadVoicesPhase.Failed;
    }
}
