using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class UltraStarSongParser
{
    public static UltraStarSongMeta ParseSongFile(string filePath, out List<SongIssue> songIssues, Encoding encoding, bool useUniversalCharsetDetector)
    {
        try
        {
            using StreamReader reader = PlainTextReader.GetFileStreamReader(filePath, encoding, useUniversalCharsetDetector);
            UltraStarSongMeta songMeta = ParseSong(reader, out songIssues);

            songMeta.SetFileInfo(filePath, reader.CurrentEncoding);

            // Log issues
            songIssues.ForEach(songIssue => Debug.LogWarning($"{songIssue.Message} in file '{filePath}'"));

            return songMeta;
        }
        catch (ExplicitEncodingMismatchException ex)
        {
            return ParseSongFile(filePath, out songIssues, ex.ExplicitlyDefinedEncoding, useUniversalCharsetDetector);
        }
    }

    public static UltraStarSongMeta ParseSong(StreamReader reader, out List<SongIssue> songIssues)
    {
        songIssues = new();

        Dictionary<string, string> requiredFields = new()
        {
            { "bpm", null },
            { "mp3", null },
            { "title", null }
        };
        Dictionary<EVoiceId, string> voiceIdToDisplayName = new();
        Dictionary<string, string> otherFields = new();

        uint lineNumber = 0;
        while (!reader.EndOfStream)
        {
            ++lineNumber;
            string line = reader.ReadLine();
            if (line.TrimStart().IsNullOrEmpty())
            {
                // Ignore empty line
                continue;
            }

            if (!line.StartsWith("#", StringComparison.InvariantCultureIgnoreCase))
            {
                if (lineNumber == 1)
                {
                    throw new UltraStarSongParserException("Does not look like a song file; ignoring");
                }

                // Finished headers
                break;
            }

            char[] separator = { ':' };
            string[] parts = line.Substring(1).Split(separator, 2);
            if (parts.Length < 2)
            {
                songIssues.Add(SongIssue.CreateWarning(null, "Invalid formatting on line " + line));
                // Ignore this line. Continue with the next line.
                continue;
            }

            string tagName = parts[0].TrimEnd();
            string tagNameLowerCase = tagName.ToLowerInvariant();
            if (tagNameLowerCase.Length < 1)
            {
                songIssues.Add(SongIssue.CreateWarning(null, "Missing tag name on line " + line));
                // Ignore this line. Continue with the next line.
                continue;
            }

            string tagValue = parts[1].TrimStart();
            if (tagValue.TrimStart().IsNullOrEmpty())
            {
                // Ignore empty tags
                continue;
            }

            if (string.Equals(tagNameLowerCase, "encoding", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(tagValue, "auto", StringComparison.InvariantCultureIgnoreCase))
            {
                Encoding explicitlyDefinedEncoding;
                try
                {
                    explicitlyDefinedEncoding = EncodingUtils.GetEncoding(tagValue);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to find encoding for explicitly specified encoding name '{tagValue}'. " +
                                   $"Using guessed encoding '{reader.CurrentEncoding}' instead. " +
                                   $"Error message: {ex.Message}");
                    explicitlyDefinedEncoding = null;
                }

                if (explicitlyDefinedEncoding != null
                    && !explicitlyDefinedEncoding.Equals(reader.CurrentEncoding))
                {
                    reader.Dispose();
                    throw new ExplicitEncodingMismatchException(explicitlyDefinedEncoding, reader.CurrentEncoding);
                }
            }
            else if (requiredFields.ContainsKey(tagNameLowerCase))
            {
                requiredFields[tagNameLowerCase] = tagValue;
            }
            else if (tagNameLowerCase.Equals("previewstart"))
            {
                otherFields[tagNameLowerCase] = tagValue;
            }
            else if (tagNameLowerCase.StartsWith("previewend"))
            {
                otherFields[tagNameLowerCase] = tagValue;
            }
            else if (tagNameLowerCase.StartsWith("p", StringComparison.Ordinal)
                     && tagNameLowerCase.Length == 2
                     && char.IsDigit(tagNameLowerCase, 1)
                     && Enum.TryParse(tagNameLowerCase.ToUpperInvariant(), out EVoiceId pTagVoiceId))
            {
                otherFields.Add(tagNameLowerCase, tagValue);
                if (!voiceIdToDisplayName.ContainsKey(pTagVoiceId))
                {
                    voiceIdToDisplayName[pTagVoiceId] = tagValue;
                }
                else
                {
                    // silently ignore already set voice names
                }
            }
            else if (tagNameLowerCase.StartsWith("duetsingerp", StringComparison.Ordinal)
                     && tagNameLowerCase.Length == 12
                     && char.IsDigit(tagNameLowerCase, 11)
                    // Get P1 resp. P2 from DUETSINGERP1 resp. DUETSINGERP2
                     && Enum.TryParse(tagNameLowerCase.Substring(10).ToUpperInvariant(), out EVoiceId duetSingerPTagVoiceId))
            {
                otherFields.Add(tagNameLowerCase, tagValue);
                if (!voiceIdToDisplayName.ContainsKey(duetSingerPTagVoiceId))
                {
                    voiceIdToDisplayName.Add(duetSingerPTagVoiceId, tagValue);
                }
                else
                {
                    // silently ignore already set voice names
                }
            }
            else
            {
                if (otherFields.ContainsKey(tagNameLowerCase))
                {
                    songIssues.Add(SongIssue.CreateWarning(null, $"Cannot set '{tagName}' multiple times"));
                }
                else
                {
                    otherFields[tagNameLowerCase] = tagValue;
                }
            }
        }

        // Check that required tags are set.
        foreach (var requiredFieldName in requiredFields)
        {
            if (requiredFieldName.Value == null)
            {
                throw new UltraStarSongParserException("Required tag '" + requiredFieldName.Key + "' was not set");
            }
        }

        try
        {
            otherFields.TryGetValue("artist", out string artist);
            if (artist == null)
            {
                artist = "";
            }

            float bpm;
            try
            {
                 bpm = ConvertToFloat(requiredFields["bpm"]);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw new UltraStarSongParserException($"Failed to parse BPM value {requiredFields["bpm"]}");
            }

            string audioFile = requiredFields["mp3"];
            string title = requiredFields["title"];
            UltraStarSongMeta songMeta = new(
                artist,
                title,
                bpm,
                audioFile,
                voiceIdToDisplayName);
            foreach (KeyValuePair<string, string> item in otherFields)
            {
                try
                {
                    ConsumeOptionalHeaderField(songMeta, item.Key, item.Value);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Failed to handle header field '{item.Key}' with value '{item.Value}'";
                    Debug.LogException(ex);
                    Debug.LogError(errorMessage);
                    songIssues.Add(SongIssue.CreateWarning(songMeta, errorMessage));
                }
            }

            // Recreate issues with proper SongMeta
            songIssues = songIssues.Select(songIssue =>
                    new SongIssue(songIssue.Severity, new SongIssueData(songMeta),
                        songIssue.Message, songIssue.StartBeat, songIssue.EndBeat))
                .ToList();

            return songMeta;
        }
        catch (ArgumentNullException e)
        {
            // if you get these with e.ParamName == "s", it's probably one of the non-nullable things (ie, float, uint, etc)
            throw new UltraStarSongParserException("Required tag '" + e.ParamName + "' was not set");
        }
    }

    private static void ConsumeOptionalHeaderField(UltraStarSongMeta songMeta, string key, string value)
    {
        switch (key)
        {
            case "background":
                songMeta.Background = value;
                break;
            case "cover":
                songMeta.Cover = value;
                break;
            case "edition":
                songMeta.Edition = value;
                break;
            case "end":
                songMeta.End = ConvertToFloat(value);
                break;
            case "gap":
                songMeta.Gap = ConvertToFloat(value);
                break;
            case "genre":
                songMeta.Genre = value;
                break;
            case "language":
                songMeta.Language = value;
                break;
            case "previewstart":
                songMeta.PreviewStart = ConvertToFloat(value);
                break;
            case "previewend":
                songMeta.PreviewEnd = ConvertToFloat(value);
                break;
            case "start":
                songMeta.Start = ConvertToFloat(value);
                break;
            case "video":
                songMeta.Video = value;
                break;
            case "videogap":
                songMeta.VideoGap = ConvertToFloat(value);
                break;
            case "year":
                songMeta.Year = ConvertToUInt32(value);
                break;
            case "medleystartbeat":
                songMeta.MedleyStartBeat = ConvertToInt32(value);
                break;
            case "medleyendbeat":
                songMeta.MedleyEndBeat = ConvertToInt32(value);
                break;
            case "audio":
                songMeta.Mp3 = value;
                break;
            case "vocalsaudio":
                songMeta.VocalsAudio = value;
                break;
            case "website":
                songMeta.Website = value;
                break;
            case "mbid_record":
                songMeta.MusicBrainzRecord = value;
                break;
            case "instrumentalaudio":
                songMeta.InstrumentalAudio = value;
                break;
            case "artist":
                songMeta.Artist = value;
                break;
            case "title":
                songMeta.Title = value;
                break;
            default:
                songMeta.SetAdditionalHeaderEntry(key, value);
                break;
        }
    }

    private static string NormalizeNumber(string s)
    {
        if (s.IsNullOrEmpty())
        {
            return s;
        }

        return s.Replace(",", ".").Trim();
    }

    private static float ConvertToFloat(string s)
    {
        // Some txt files use comma as decimal separator (e.g. "12,34" instead "12.34").
        // Convert this to English notation.
        string sNormalized = NormalizeNumber(s);
        if (float.TryParse(sNormalized, NumberStyles.Any, CultureInfo.InvariantCulture, out float res))
        {
            return res;
        }
        else
        {
            throw new UltraStarSongParserException($"Could not convert string '{s}' to a float.");
        }
    }

    private static uint ConvertToUInt32(string s)
    {
        if (s.IsNullOrEmpty())
        {
            return 0;
        }

        string sNormalized = NormalizeNumber(s);
        try
        {
            return Convert.ToUInt32(sNormalized, 10);
        }
        catch (FormatException e)
        {
            throw new UltraStarSongParserException("Could not convert " + s + " to an uint. Reason: " + e.Message, e);
        }
    }

    private static int ConvertToInt32(string s)
    {
        if (s.IsNullOrEmpty())
        {
            return 0;
        }

        string sNormalized = NormalizeNumber(s);
        try
        {
            return Convert.ToInt32(sNormalized, 10);
        }
        catch (FormatException e)
        {
            throw new UltraStarSongParserException("Could not convert " + s + " to an int. Reason: " + e.Message, e);
        }
    }

    public class ExplicitEncodingMismatchException : Exception
    {
        public Encoding ExplicitlyDefinedEncoding { get; private set; }

        public ExplicitEncodingMismatchException(Encoding explicitlyDefinedEncoding, Encoding otherEncoding)
            : base($"Encoding used to parse song '{otherEncoding}' " +
                   $"does not match explicitly defined encoding '{explicitlyDefinedEncoding}'")
        {
            ExplicitlyDefinedEncoding = explicitlyDefinedEncoding;
        }
    }
}
