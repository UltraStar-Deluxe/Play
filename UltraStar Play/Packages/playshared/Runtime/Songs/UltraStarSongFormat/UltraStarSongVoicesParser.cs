using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class UltraStarSongVoicesParser
{
    private static readonly char[] noteSegmentSeparators = { ' ' };

    private readonly StreamReader streamReader;
    private readonly bool isRelativeSongFormat;
    private readonly string filePath;

    private readonly Dictionary<EVoiceId, Voice> voiceIdToVoiceMap = new();

    private Voice currentVoice;
    private Sentence currentSentence;
    private bool endFound;

    // The last beat is only relevant for relative song files. Any beat will be relative to this.
    private int lastBeat;

    public static List<Voice> ParseFile(string filePath, Encoding fileEncoding, bool isRelativeSongFormat, bool useUniversalCharsetDetector)
    {
        StreamReader reader = PlainTextReader.GetFileStreamReader(filePath, fileEncoding, useUniversalCharsetDetector);
        UltraStarSongVoicesParser parser = new(reader, isRelativeSongFormat, filePath);
        IReadOnlyList<Voice> voices = parser.Parse();
        return new List<Voice>(voices);
    }

    public static List<Voice> ParseString(string text, bool isRelativeSongFormat)
    {
        MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        StreamReader streamReader = new StreamReader(memoryStream, Encoding.UTF8);
        return ParseStreamReader(streamReader, isRelativeSongFormat);
    }

    private static List<Voice> ParseStreamReader(StreamReader reader, bool isRelativeSongFormat)
    {
        UltraStarSongVoicesParser parser = new(reader, isRelativeSongFormat);
        IReadOnlyList<Voice> voices = parser.Parse();
        return new List<Voice>(voices);
    }

    private UltraStarSongVoicesParser(
        StreamReader reader,
        bool isRelativeSongFormat,
        string filePath = null)
    {
        this.streamReader = reader;
        this.isRelativeSongFormat = isRelativeSongFormat;
        this.filePath = filePath.NullToEmpty();
        currentVoice = new Voice(EVoiceId.P1);
        voiceIdToVoiceMap.Add(EVoiceId.P1, currentVoice);
    }

    private IReadOnlyList<Voice> Parse()
    {
        try
        {
            uint lineNumber = 0;
            while (!endFound && !streamReader.EndOfStream)
            {
                lineNumber++;
                string line = streamReader.ReadLine();
                ParseLine(line, lineNumber);
            }

            return new List<Voice>(voiceIdToVoiceMap.Values);
        }
        finally
        {
            streamReader.Close();
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
                LogLineError(lineNumber, "Invalid instruction: " + line);
                break;
        }
    }

    private void ParseNote(string line, uint lineNumber)
    {
        // Create sentence if needed.
        if (currentVoice == null)
        {
            LogLineError(lineNumber, "Note encountered but no voice is active");
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
            LogLineError(lineNumber, e.Message, e);
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
            LogLineError(lineNumber, e.Message, e);
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
            LogLineError(lineNumber, "Voice id is null or empty, should be 'P1' or 'P2' for example");
        }

        // Normalize voice name.
        // Most use "P1", "P2", etc.
        // But some use "P 1", "P 2", etc. (with spaces)
        string normalizedVoiceIdString = voiceIdString.Replace(" ", "");

        if (!Enum.TryParse(normalizedVoiceIdString, out EVoiceId voiceId))
        {
            LogLineError(lineNumber, $"Failed to parse voice id '{voiceIdString}', should be 'P1' or 'P2' for example");
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
        List<string> data = ParseNoteSegments(line.TrimStart());
        if (data.Count < 4)
        {
            throw new UltraStarSongParserException(GetIncompleteNoteErrorMessage(line));
        }

        string noteTypeString = data[0];
        string startBeatString = data[1];
        string lengthString = data[2];
        string txtPitchString = data[3];
        // Lyrics and thus also the last separator are optional.
        string lyricsString = data.Count >= 5 ? data[4] : "";

        if (noteTypeString.IsNullOrEmpty()
            || startBeatString.IsNullOrEmpty()
            || lengthString.IsNullOrEmpty()
            || txtPitchString.IsNullOrEmpty())
        {
            throw new UltraStarSongParserException(GetIncompleteNoteErrorMessage(line));
        }

        ENoteType noteType = GetNoteType(noteTypeString);
        int startBeat = ConvertToBeat(startBeatString);
        lastBeat = startBeat;
        int length = ConvertToInt32(lengthString);
        int txtPitch = ConvertToInt32(txtPitchString);
        string lyrics = lyricsString;
        return new Note(
            noteType,
            startBeat,
            length,
            txtPitch,
            lyrics
        );
    }

    private List<string> ParseNoteSegments(string line)
    {
        if (line.IsNullOrEmpty())
        {
            return new List<string>();
        }

        // NoteType is known to be exactly one character long
        List<StringBuilder> stringBuilders = new();
        stringBuilders.Add(new StringBuilder().Append(line[0]));

        StringBuilder currentStringBuilder = new StringBuilder();
        stringBuilders.Add(currentStringBuilder);
        // Start at index 1 because 0 was the NoteType
        for (int i = 1; i < line.Length; i++)
        {
            char c = line[i];
            switch (c)
            {
                case ' ':
                    if (stringBuilders.Count < 4)
                    {
                        // Still looking for more parts
                        if (currentStringBuilder.Length > 0)
                        {
                            currentStringBuilder = new StringBuilder();
                            stringBuilders.Add(currentStringBuilder);
                        }
                    }
                    else if (currentStringBuilder.Length > 0)
                    {
                        // Found all parts, the rest is lyrics
                        currentStringBuilder = new StringBuilder();
                        stringBuilders.Add(currentStringBuilder);
                        for (i++; i < line.Length; i++)
                        {
                            currentStringBuilder.Append(line[i]);
                        }
                    }
                    break;
                default:
                    currentStringBuilder.Append(c);
                    break;
            }
        }
        return stringBuilders
            .Select(stringBuilder => stringBuilder.ToString())
            .ToList();
    }

    private static string GetIncompleteNoteErrorMessage(string line)
    {
        return $"Incomplete note. Got '{line}' but expected '<NoteType> <StartBeat> <LengthInBeats> <Pitch> <Lyrics>'";
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

    private void LogLineError(uint lineNumber, string message, Exception exception = null)
    {
        if (exception != null)
        {
            Debug.LogException(exception);
        }
        Debug.LogWarning($"{message} (path: '{filePath}', line: {lineNumber}, encoding: {streamReader.CurrentEncoding})");
    }

    private void LogLineWarning(uint lineNumber, string message)
    {
        Debug.LogWarning($"{message} (path: '{filePath}', line: {lineNumber}, encoding: {streamReader.CurrentEncoding})");
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
