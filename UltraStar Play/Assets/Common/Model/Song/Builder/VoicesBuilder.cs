using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class VoicesBuilder
{
    private Voice currentVoice;
    private Sentence currentSentence;
    private bool endFound;

    private readonly Dictionary<string, Voice> voiceNameToVoiceMap = new Dictionary<string, Voice>();

    private readonly bool isRelativeSongFile;
    // The last beat is only relevant for relative song files. Any beat will be relative to this.
    private int lastBeat;

    public VoicesBuilder(string path, Encoding enc, bool isRelativeSongFile)
    {
        this.isRelativeSongFile = isRelativeSongFile;
        currentVoice = new Voice(Voice.soloVoiceName);
        voiceNameToVoiceMap.Add(Voice.soloVoiceName, currentVoice);

        using (StreamReader reader = TxtReader.GetFileStreamReader(path, enc))
        {
            ParseStreamReader(reader);
        }
    }

    public IReadOnlyList<Voice> GetVoices()
    {
        return new List<Voice>(voiceNameToVoiceMap.Values);
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
        catch (VoicesBuilderException e)
        {
            ThrowLineError(lineNumber, e.Message);
        }
    }

    private void ParseSentenceEnd(string line, uint lineNumber)
    {
        if (currentSentence == null)
        {
            ThrowLineError(lineNumber, "Linebreak encountered without preceding notes");
        }

        try
        {
            ParseSentenceStartBeatAndEndBeat(line, out int startBeat, out int endBeat);
            currentSentence.SetLinebreakBeat(startBeat);
            currentSentence = null;
        }
        catch (VoicesBuilderException e)
        {
            ThrowLineError(lineNumber, e.Message);
        }
    }

    private void ParseSentenceStartBeatAndEndBeat(string line, out int startBeat, out int endBeat)
    {
        // Format of line breaks: - STARTBEAT ENDBEAT
        // Thereby, ENDBEAT is optional.
        char[] splitChars = { ' ' };
        string[] data = line.Split(splitChars);

        startBeat = 0;
        endBeat = 0;
        if (data.Length == 3)
        {
            string startBeatText = data[1];
            startBeat = ConvertToBeat(startBeatText);
            // TODO: Store endBeatText in SongMeta.
            string endBeatText = data[2];
            endBeat = ConvertToBeat(endBeatText);
            lastBeat = endBeat;
        }
        else if (data.Length == 2)
        {
            string startBeatText = data[1];
            startBeat = ConvertToBeat(startBeatText);
            lastBeat = startBeat;
        }
        else
        {
            throw new VoicesBuilderException("Invalid instruction: " + line);
        }
    }

    private void ParseVoiceStart(string voiceName, uint lineNumber)
    {
        if (voiceName.IsNullOrEmpty())
        {
            ThrowLineError(lineNumber, "Voice name is empty");
        }

        // Remove the default voice for solo songs.
        voiceNameToVoiceMap.Remove(Voice.soloVoiceName);

        // switch to or create new voice
        if (!voiceNameToVoiceMap.TryGetValue(voiceName, out Voice nextVoice))
        {
            // Voice has not been found, so create new one.
            nextVoice = new Voice(voiceName);
            voiceNameToVoiceMap.Add(voiceName, nextVoice);
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
            throw new VoicesBuilderException("Incomplete note");
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
        if (isRelativeSongFile)
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
                throw new VoicesBuilderException("Cannot convert '" + s + "' to a ENoteType");
        }
        return res;
    }

    private static void ThrowLineError(uint lineNumber, string message)
    {
        throw new VoicesBuilderException("Error at line " + lineNumber + ": " + message);
    }

    private static int ConvertToInt32(string s)
    {
        try
        {
            return Convert.ToInt32(s.Trim(), 10);
        }
        catch (Exception e)
        {
            throw new VoicesBuilderException("Could not convert '" + s + "' to an int. Reason: " + e.Message, e);
        }
    }
}

[Serializable]
public class VoicesBuilderException : Exception
{
    public VoicesBuilderException()
    {
    }

    public VoicesBuilderException(string message)
        : base(message)
    {
    }

    public VoicesBuilderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
