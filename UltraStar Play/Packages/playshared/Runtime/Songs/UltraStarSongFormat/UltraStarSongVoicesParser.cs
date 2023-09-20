using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class UltraStarSongVoicesParser
{
    private readonly string filePath;
    private readonly Encoding encoding;
    private Voice currentVoice;
    private Sentence currentSentence;
    private bool endFound;

    private readonly Dictionary<EVoiceId, Voice> voiceIdToVoiceMap = new();

    private readonly bool isRelativeSongFormat;
    // The last beat is only relevant for relative song files. Any beat will be relative to this.
    private int lastBeat;

    public static List<Voice> ParseSongFile(string filePath, Encoding fileEncoding, bool isRelativeSongFormat, bool useUniversalCharsetDetector)
    {
        using IDisposable d = new DisposableStopwatch($"Parsing voices of '{filePath}' took <millis> ms");
        UltraStarSongVoicesParser parser = new(filePath, fileEncoding, isRelativeSongFormat, useUniversalCharsetDetector);
        IReadOnlyList<Voice> voices = parser.GetVoices();
        return new List<Voice>(voices);
    }

    private UltraStarSongVoicesParser(string filePath, Encoding encoding, bool isRelativeSongFormat, bool useUniversalCharsetDetector)
    {
        this.filePath = filePath;
        this.encoding = encoding;
        this.isRelativeSongFormat = isRelativeSongFormat;
        currentVoice = new Voice(EVoiceId.P1);
        voiceIdToVoiceMap.Add(EVoiceId.P1, currentVoice);

        if (filePath == null
            || !File.Exists(filePath))
        {
            // Nothing to load. This could be a generated song meta.
            return;
        }

        using StreamReader reader = PlainTextReader.GetFileStreamReader(filePath, encoding, useUniversalCharsetDetector);
        ParseStreamReader(reader);
    }

    private IReadOnlyList<Voice> GetVoices()
    {
        return new List<Voice>(voiceIdToVoiceMap.Values);
    }

    private void ParseStreamReader(StreamReader reader)
    {
        uint lineNumber = 0;
        while (!endFound && !reader.EndOfStream)
        {
            lineNumber++;
            string line = reader.ReadLine();
            ParseLine(line, lineNumber);
        }
    }

    private void ParseLine(string line, uint lineNumber)
    {
        // Ignore empty lines
        if (line.IsNullOrEmpty()
            || line.TrimStart().IsNullOrEmpty())
        {
            return;
        }

        switch (line[0])
        {
            case '#':
                // headers are ignored at this stage
                break;
            case 'E':
                // now we are done
                endFound = true;
                break;
            case 'P':
                // Switch to voice with that name
                ParseVoiceStart(line, lineNumber);
                break;
            case '-':
                ParseSentenceEnd(line, lineNumber);
                break;
            case ':': // Normal note
            case '*': // Golden note
            case 'F': // Freestyle note
            case 'R': // Rap note
            case 'G': // RapGolden note
                ParseNote(line, lineNumber);
                break;
            default:
                ThrowLineError(lineNumber, "Invalid instruction: " + line);
                break;
        }
    }

    private void ParseNote(string line, uint lineNumber)
    {
        // Create sentence if needed.
        if (currentVoice == null)
        {
            ThrowLineError(lineNumber, "Note encountered but no voice is active");
        }
        else if (currentSentence == null)
        {
            currentSentence = new Sentence();
            currentSentence.SetVoice(currentVoice);
        }

        // Add new note to current sentence.
        try
        {
            Note note = CreateNote(line);
            currentSentence.AddNote(note);
        }
        catch (Exception e)
        {
            ThrowLineError(lineNumber, e.Message, e);
        }
    }

    private void ParseSentenceEnd(string line, uint lineNumber)
    {
        if (currentSentence == null)
        {
            LogLineWarning(lineNumber, "Linebreak encountered without preceding notes");
            return;
        }

        try
        {
            ParseSentenceStartBeatAndEndBeat(line, out int previousSentenceEndBeat, out int nextSentenceStartBeat);
            if (previousSentenceEndBeat >= 0)
            {
                currentSentence.SetLinebreakBeat(previousSentenceEndBeat);
            }
            currentSentence = null;
        }
        catch (Exception e)
        {
            ThrowLineError(lineNumber, e.Message, e);
        }
    }

    private void ParseSentenceStartBeatAndEndBeat(string line, out int previousSentenceEndBeat, out int nextSentenceStartBeat)
    {
        // Format of line breaks: - previousSentenceEndBeat nextSentenceStartBeat
        // Thereby, previousSentenceEndBeat and nextSentenceStartBeat are optional.
        char[] splitChars = { ' ' };
        string[] data = line.Trim().Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

        previousSentenceEndBeat = -1;
        nextSentenceStartBeat = -1;
        if (data.Length == 3)
        {
            string startBeatText = data[1];
            previousSentenceEndBeat = ConvertToBeat(startBeatText);
            // TODO: Store endBeatText in SongMeta as ExtendedStartBeat ?
            string endBeatText = data[2];
            nextSentenceStartBeat = ConvertToBeat(endBeatText);
            lastBeat = nextSentenceStartBeat;
        }
        else if (data.Length == 2)
        {
            string startBeatText = data[1];
            previousSentenceEndBeat = ConvertToBeat(startBeatText);
            lastBeat = previousSentenceEndBeat;
        }
    }

    private void ParseVoiceStart(string voiceIdString, uint lineNumber)
    {
        if (voiceIdString.IsNullOrEmpty())
        {
            ThrowLineError(lineNumber, "Voice id is null or empty, should be 'P1' or 'P2' for example");
        }

        // Normalize voice name.
        // Most use "P1", "P2", etc.
        // But some use "P 1", "P 2", etc. (with spaces)
        string normalizedVoiceIdString = voiceIdString.Replace(" ", "");

        if (!Enum.TryParse(normalizedVoiceIdString, out EVoiceId voiceId))
        {
            ThrowLineError(lineNumber, $"Failed to parse voice id '{voiceIdString}', should be 'P1' or 'P2' for example");
        }

        // Switch to or create new voice
        if (!voiceIdToVoiceMap.TryGetValue(voiceId, out Voice nextVoice))
        {
            // Voice has not been found, so create new one.
            nextVoice = new Voice(voiceId);
            voiceIdToVoiceMap.Add(voiceId, nextVoice);
        }
        currentVoice = nextVoice;
        currentSentence = null;
    }

    private Note CreateNote(string line)
    {
        char[] splitChars = { ' ' };
        string[] data = line.Split(splitChars, 5);
        if (data.Length < 5)
        {
            throw new UltraStarSongParserException("Incomplete note");
        }
        ENoteType noteType = GetNoteType(data[0]);
        int startBeat = ConvertToBeat(data[1]);
        lastBeat = startBeat;
        int length = ConvertToInt32(data[2]);
        int txtPitch = ConvertToInt32(data[3]);
        string lyrics = data[4];
        return new Note(
            noteType,
            startBeat,
            length,
            txtPitch,
            lyrics
        );
    }

    private int ConvertToBeat(string s)
    {
        int beat = ConvertToInt32(s);
        if (isRelativeSongFormat)
        {
            beat += lastBeat;
        }
        return beat;
    }

    private static ENoteType GetNoteType(string s)
    {
        ENoteType res;
        switch (s)
        {
            case ":":
                res = ENoteType.Normal;
                break;
            case "*":
                res = ENoteType.Golden;
                break;
            case "F":
                res = ENoteType.Freestyle;
                break;
            case "R":
                res = ENoteType.Rap;
                break;
            case "G":
                res = ENoteType.RapGolden;
                break;
            default:
                throw new UltraStarSongParserException("Cannot convert '" + s + "' to a ENoteType");
        }
        return res;
    }

    private void ThrowLineError(uint lineNumber, string message, Exception innerException = null)
    {
        throw new UltraStarSongParserException(
            $"{message} (path: '{filePath}', line: {lineNumber}, encoding: {encoding})",
            innerException);
    }

    private void LogLineWarning(uint lineNumber, string message)
    {
        Debug.LogWarning(message
                         + " (path: '" + filePath + "', " +
                         "line: " + lineNumber + ", " +
                         "encoding: " + encoding + ")");
    }

    private static int ConvertToInt32(string s)
    {
        try
        {
            return Convert.ToInt32(s.Trim(), 10);
        }
        catch (Exception e)
        {
            throw new UltraStarSongParserException("Could not convert '" + s + "' to an int. Reason: " + e.Message, e);
        }
    }
}
