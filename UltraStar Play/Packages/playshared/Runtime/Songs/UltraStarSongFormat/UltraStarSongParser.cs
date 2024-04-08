using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class UltraStarSongParser
{
    public static UltraStarSongMeta ParseFile(
        string filePath,
        out List<SongIssue> songIssues,
        Encoding encoding = null,
        bool useUniversalCharsetDetector = true,
        bool logIssues = true)
    {
        try
        {
            using StreamReader reader = PlainTextReader.GetFileStreamReader(filePath, encoding, useUniversalCharsetDetector);
            UltraStarSongMeta songMeta = ParseStreamReader(reader, out songIssues);

            songMeta.SetFileInfo(filePath, reader.CurrentEncoding);

            // Log issues
            if (logIssues)
            {
                songIssues.ForEach(songIssue => Debug.LogWarning($"{songIssue.Message} in file '{filePath}'"));
            }

            // Lazy load voices
            songMeta.DoLoadVoices = () =>
            {
                using IDisposable d = new DisposableStopwatch($"Loading voices of '{filePath}' took <ms> ms");
                List<Voice> voices = UltraStarSongVoicesParser.ParseFile(
                    songMeta.FileInfo.FullName,
                    songMeta.FileEncoding,
                    songMeta.IsTxtFileRelative,
                    false);
                voices.ForEach(voice => songMeta.AddVoice(voice));
            };
            return songMeta;
        }
        catch (ExplicitEncodingMismatchException ex)
        {
            return ParseFile(filePath, out songIssues, ex.ExplicitlyDefinedEncoding, useUniversalCharsetDetector, logIssues);
        }
    }

    public static UltraStarSongMeta ParseString(string text, out List<SongIssue> songIssues, bool logIssues = true)
    {
        using MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using StreamReader streamReader = new StreamReader(memoryStream, Encoding.UTF8);
        UltraStarSongMeta songMeta = ParseStreamReader(streamReader, out songIssues);

        if (logIssues)
        {
            // Log issues
            songIssues.ForEach(songIssue => Debug.LogWarning($"{songIssue.Message} in song '{SongMetaUtils.GetArtistDashTitle(songMeta)}'"));
        }

        // Lazy load voices
        songMeta.DoLoadVoices = () =>
        {
            List<Voice> voices = UltraStarSongVoicesParser.ParseString(
                text,
                songMeta.IsTxtFileRelative);
            voices.ForEach(voice => songMeta.AddVoice(voice));
        };

        return songMeta;
    }

    private static UltraStarSongMeta ParseStreamReader(StreamReader reader, out List<SongIssue> songIssues)
    {
        songIssues = new();

        Dictionary<string, string> headerFields = ParseHeaderFields(reader, songIssues);
        NormalizeHeaderFields(headerFields);

        if (headerFields.TryGetValue("ENCODING", out string explicitlyDefinedEncodingName))
        {
            CheckEncodingHeaderMatches(explicitlyDefinedEncodingName, reader);
        }

        AddSongIssuesForMissingMandatoryHeaderFields(headerFields, songIssues);
        Dictionary<EVoiceId, string> voiceIdToDisplayName = GetCustomVoiceIdDisplayNames(headerFields);

        float txtFileBpm = GetTxtFileBpm(headerFields);
        UltraStarSongFormatVersion version = new(headerFields.GetValueOrDefault("VERSION", ""));

        UltraStarSongMeta songMeta = new(
            headerFields.GetValueOrDefault("ARTIST", ""),
            headerFields.GetValueOrDefault("TITLE", ""),
            txtFileBpm,
            headerFields.GetValueOrDefault("AUDIO", ""),
            voiceIdToDisplayName,
            version);
        foreach (KeyValuePair<string, string> item in headerFields)
        {
            try
            {
                ApplyOptionalHeaderField(songMeta, item.Key, item.Value);
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

    private static float GetTxtFileBpm(Dictionary<string,string> headerFields)
    {
        if (headerFields.TryGetValue("BPM", out string txtFileBpmString))
        {
            return ParseFloat("BPM", txtFileBpmString);
        }
        return 0;
    }

    private static void AddSongIssuesForMissingMandatoryHeaderFields(Dictionary<string,string> headerFields, List<SongIssue> songIssues)
    {
        List<List<string>> mandatoryHeaderFieldsWithAlternatives = new List<List<string>>()
        {
            new List<string>() { "AUDIO", "AUDIOURL", "VIDEOURL", "WEBSITE", },
            new List<string>() { "BPM" },
            new List<string>() { "TITLE" },
        };

        foreach (List<string> mandatoryHeaderFieldWithAlternatives in mandatoryHeaderFieldsWithAlternatives)
        {
            if (mandatoryHeaderFieldWithAlternatives.AllMatch(mandatoryHeaderField =>
                    !headerFields.TryGetValue(mandatoryHeaderField, out string mandatoryHeaderFieldValue)
                    || mandatoryHeaderFieldValue.IsNullOrEmpty()))
            {
                songIssues.Add(SongIssue.CreateError(null,
                    $"Missing required header field. Specify {mandatoryHeaderFieldsWithAlternatives.JoinWith(" or ")}"));
            }
        }
    }

    private static Dictionary<EVoiceId,string> GetCustomVoiceIdDisplayNames(Dictionary<string,string> headerFields)
    {
        Dictionary<EVoiceId, string> result = new();
        foreach (KeyValuePair<string,string> headerField in headerFields)
        {
            if (TryParseCustomVoiceIdDisplayNameHeader(headerField.Key, headerField.Value, out EVoiceId voiceId, out string displayName))
            {
                result[voiceId] = displayName;
            }
        }
        return result;
    }

    private static bool TryParseCustomVoiceIdDisplayNameHeader(string key, string value, out EVoiceId voiceId, out string displayName)
    {
        if (key.StartsWith('P')
            && key.Length == 2
            && char.IsDigit(key, 1)
            && Enum.TryParse(key, out voiceId))
        {
            displayName = value;
            return true;
        }

        voiceId = EVoiceId.P1;
        displayName = "";
        return false;
    }

    private static void CheckEncodingHeaderMatches(string explicitlyDefinedEncodingName, StreamReader reader)
    {
        if (explicitlyDefinedEncodingName.IsNullOrEmpty()
            && string.Equals(explicitlyDefinedEncodingName, "auto", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        Encoding explicitlyDefinedEncoding;
        try
        {
            explicitlyDefinedEncoding = EncodingUtils.GetEncoding(explicitlyDefinedEncodingName);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to find encoding for name '{explicitlyDefinedEncodingName}'. " +
                           $"Using guessed encoding '{reader.CurrentEncoding}' instead. " +
                           $"Error message: {ex.Message}");
            explicitlyDefinedEncoding = null;
        }

        if (explicitlyDefinedEncoding == null
            || explicitlyDefinedEncoding.Equals(reader.CurrentEncoding))
        {
            return;
        }

        throw new ExplicitEncodingMismatchException(explicitlyDefinedEncoding, reader.CurrentEncoding);
    }

    private static Dictionary<string,string> ParseHeaderFields(StreamReader reader, List<SongIssue> songIssues)
    {
        Dictionary<string, string> headerFields = new(StringComparer.InvariantCultureIgnoreCase);

        uint lineNumber = 0;
        while (!reader.EndOfStream)
        {
            lineNumber++;
            string line = reader.ReadLine();
            if (TryParseHeaderField(line, out string key, out string value))
            {
                if (headerFields.ContainsKey(key))
                {
                    songIssues.Add(SongIssue.CreateWarning(null, $"Cannot set '{key}' multiple times"));
                }
                else
                {
                    headerFields.Add(key, value);
                }
            }
            else if (lineNumber == 1)
            {
                // Headers are mandatory
                throw new UltraStarSongParserException("Does not look like a song file, ignoring");
            }
            else if (line.IsNullOrEmpty())
            {
                // Ignore empty line
                continue;
            }
            else if (line.StartsWith('#'))
            {
                // This could be a header fields with invalid syntax, e.g., using the wrong separator for key and value.
                songIssues.Add(SongIssue.CreateWarning(null, $"Invalid formatting of header field on line {lineNumber}: '{line}'"));
            }
            else
            {
                // Reached end of header fields
                break;
            }
        }

        return headerFields;
    }

    private static bool TryParseHeaderField(string line, out string key, out string value)
    {
        key = null;
        value = null;
        if (line.IsNullOrEmpty()
            || !line.StartsWith('#'))
        {
            return false;
        }

        int indexOfSeparator = line.IndexOf(':');
        if (indexOfSeparator <= 2)
        {
            return false;
        }

        key = line.Substring(1, indexOfSeparator -1).Trim();
        value = line.Substring(indexOfSeparator + 1, line.Length - indexOfSeparator - 1).Trim();

        return !key.IsNullOrEmpty();
    }

    private static void NormalizeHeaderFields(Dictionary<string, string> headerFields)
    {
        NormalizeHeaderFieldKeys(headerFields);
    }

    private static void NormalizeHeaderFieldKeys(Dictionary<string,string> headerFields)
    {
        Dictionary<string, string> headerKeyToNormalizedHeaderKey = new Dictionary<string, string>()
        {
            { "MP3", "AUDIO" },
            { "INSTRUMENTALAUDIO", "INSTRUMENTAL" },
            { "VOCALSAUDIO", "VOCALS" },
            { "AUDIOGAP", "GAP" },
            { "DUETSINGERP1", "P1" },
            { "DUETSINGERP2", "P2" },
        };

        foreach (KeyValuePair<string,string> entry in headerKeyToNormalizedHeaderKey)
        {
            string headerKey = entry.Key;
            string normalizedHeaderKey = entry.Value;
            if (headerFields.TryGetValue(headerKey, out string value))
            {
                headerFields.Remove(headerKey);
                headerFields.Add(normalizedHeaderKey, value);
            }
        }
    }

    private static void ApplyOptionalHeaderField(UltraStarSongMeta songMeta, string key, string value)
    {
        switch (key)
        {
            case "ARTIST":
                songMeta.Artist = value;
                break;
            case "AUDIO":
                songMeta.Audio = value;
                break;
            case "BACKGROUND":
                songMeta.Background = value;
                break;
            case "COVER":
                songMeta.Cover = value;
                break;
            case "EDITION":
                songMeta.Edition = value;
                break;
            case "END":
                songMeta.TxtFileEndInMillis = ParseFloat(key, value);
                break;
            case "GAP":
                songMeta.GapInMillis = ParseFloat(key, value);
                break;
            case "GENRE":
                songMeta.Genre = value;
                break;
            case "INSTRUMENTAL":
                songMeta.InstrumentalAudio = value;
                break;
            case "LANGUAGE":
                songMeta.Language = value;
                break;
            case "MEDLEYENDBEAT":
                songMeta.TxtFileMedleyEndBeat = ParseInt32(key, value);
                break;
            case "MEDLEYSTARTBEAT":
                songMeta.TxtFileMedleyStartBeat = ParseInt32(key, value);
                break;
            case "PREVIEWEND":
                songMeta.TxtFilePreviewEndInSeconds = ParseFloat(key, value);
                break;
            case "PREVIEWSTART":
                songMeta.TxtFilePreviewStartInSeconds = ParseFloat(key, value);
                break;
            case "STAR":
                songMeta.TxtFileStartInSeconds = ParseFloat(key, value);
                break;
            case "TITLE":
                songMeta.Title = value;
                break;
            case "VIDEO":
                songMeta.Video = value;
                break;
            case "VIDEOGAP":
                songMeta.TxtFileVideoGapInSeconds = ParseFloat(key, value);
                break;
            case "VOCALS":
                songMeta.VocalsAudio = value;
                break;
            case "WEBSITE":
                songMeta.Website = value;
                break;
            case "YEAR":
                songMeta.Year = ParseUInt32(key, value);
                break;
            default:
                songMeta.SetAdditionalHeaderEntry(key, value);
                break;
        }
    }

    /**
     * Normalizes the decimal separator and surrounding whitespace.
     */
    private static string NormalizeNumber(string s)
    {
        if (s.IsNullOrEmpty())
        {
            return "";
        }

        return s.Replace(",", ".").Trim();
    }

    private static float ParseFloat(string headerFieldName, string value)
    {
        if (value.IsNullOrEmpty())
        {
            return 0;
        }

        string normalizedValue = NormalizeNumber(value);
        if (float.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float res))
        {
            return res;
        }
        else
        {
            throw new UltraStarSongParserException($"Failed to parse float. Header field: {headerFieldName}, value: {value}");
        }
    }

    private static uint ParseUInt32(string headerFieldName, string value)
    {
        if (value.IsNullOrEmpty())
        {
            return 0;
        }

        string valueNormalized = NormalizeNumber(value);
        if (uint.TryParse(valueNormalized, out uint res))
        {
            return res;
        }
        else
        {
            throw new UltraStarSongParserException($"Failed to parse uint. Header field: {headerFieldName}, value: {value}");
        }
    }

    private static int ParseInt32(string headerFieldName, string value)
    {
        if (value.IsNullOrEmpty())
        {
            return 0;
        }

        string normalizedValue = NormalizeNumber(value);
        if (int.TryParse(normalizedValue, out int res))
        {
            return res;
        }
        else
        {
            throw new UltraStarSongParserException($"Failed to parse int. Header field: {headerFieldName}, value: {value}");
        }
    }

    public class ExplicitEncodingMismatchException : Exception
    {
        public Encoding ExplicitlyDefinedEncoding { get; private set; }

        public ExplicitEncodingMismatchException(Encoding explicitlyDefinedEncoding, Encoding otherEncoding)
            : base($"Encoding used to parse song '{otherEncoding}' " +
                   $"does not match explicitly defined explicitlyDefinedEncodingName '{explicitlyDefinedEncoding}'")
        {
            ExplicitlyDefinedEncoding = explicitlyDefinedEncoding;
        }
    }
}
