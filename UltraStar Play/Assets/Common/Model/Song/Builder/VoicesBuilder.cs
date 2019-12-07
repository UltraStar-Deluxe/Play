using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

static class VoicesBuilder
{
    public static Dictionary<string, Voice> ParseFile(string path, Encoding enc, IEnumerable<string> voiceIdentifiers)
    {
        Dictionary<string, MutableVoice> res = new Dictionary<string, MutableVoice>();
        foreach (string voiceIdentifier in voiceIdentifiers)
        {
            res.Add(voiceIdentifier, new MutableVoice());
        }
        MutableVoice currentVoice = null;
        MutableSentence currentSentence = null;
        bool endFound = false;

        // if this is a solo song (without a named voice) then just add one with identifier "" (empty string)
        string soloVoiceIdentifier = "";
        if (res.Count == 0)
        {
            res.Add(soloVoiceIdentifier, new MutableVoice());
            currentVoice = res[""];
        }

        using (StreamReader reader = TxtReader.GetFileStreamReader(path, enc))
        {
            uint lineNumber = 0;
            while (!endFound && !reader.EndOfStream)
            {
                ++lineNumber;
                string line = reader.ReadLine();
                switch (line[0])
                {
                    case '#':
                        // headers are ignored at this stage
                        break;
                    case 'E':
                        // finish any current open sentence
                        if (currentVoice != null && currentSentence != null)
                        {
                            currentVoice.Add((Sentence)currentSentence);
                        }
                        // now we are done
                        endFound = true;
                        break;
                    case 'P':
                        // save the current sentence, if any
                        if (currentVoice != null && currentSentence != null)
                        {
                            currentVoice.Add((Sentence)currentSentence);
                        }
                        // switch to or create new voice
                        if (!res.TryGetValue(line, out MutableVoice nextVoice))
                        {
                            // Voice has not been found, so create new one.
                            nextVoice = new MutableVoice();
                            res.Add(line, nextVoice);
                        }
                        currentVoice = nextVoice;
                        currentSentence = null;
                        // Remove the default voice for solo songs.
                        res.Remove(soloVoiceIdentifier);
                        break;
                    case '-':
                        if (currentVoice == null)
                        {
                            ThrowLineError(lineNumber, "Linebreak encountered but no voice is active");
                        }
                        else if (currentSentence == null)
                        {
                            ThrowLineError(lineNumber, "Linebreak encountered without preceding notes");
                        }
                        try
                        {
                            currentSentence.SetLinebreakBeat(ParseLinebreak(line.Substring(2)));
                            currentVoice.Add((Sentence)currentSentence);
                            currentSentence = null;
                        }
                        catch (VoicesBuilderException e)
                        {
                            ThrowLineError(lineNumber, e.Message);
                        }
                        break;
                    case ':':
                    case '*':
                    case 'F':
                    case 'R':
                    case 'G':
                        if (currentVoice == null)
                        {
                            ThrowLineError(lineNumber, "Note encountered but no voice is active");
                        }
                        else if (currentSentence == null)
                        {
                            currentSentence = new MutableSentence();
                        }
                        try
                        {
                            currentSentence.Add(ParseNote(line));
                        }
                        catch (VoicesBuilderException e)
                        {
                            ThrowLineError(lineNumber, e.Message);
                        }
                        break;
                    default:
                        ThrowLineError(lineNumber, "Invalid instruction: " + line);
                        break;
                }
            }
        }

        Dictionary<string, Voice> actualRes = new Dictionary<string, Voice>();
        foreach (var item in res)
        {
            actualRes.Add(item.Key, (Voice)item.Value);
        }
        return actualRes;
    }

    private static int ParseLinebreak(string line)
    {
        // Format of line breaks: - STARTBEAT ENDBEAT
        // Thereby, ENDBEAT is optional.
        int indexOfSpace = line.IndexOf(" ", StringComparison.InvariantCulture);
        if (indexOfSpace >= 0)
        {
            string startBeatText = line.Substring(0, indexOfSpace + 1);
            // TODO: Store endBeatText in SongMeta.
            // string endBeatText = line.Substring(indexOfSpace + 1, line.Length - (indexOfSpace + 1));
            return ConvertToInt32(startBeatText);
        }
        else
        {
            return ConvertToInt32(line);
        }

    }

    private static Note ParseNote(string line)
    {
        char[] splitChars = { ' ' };
        string[] data = line.Split(splitChars, 5);
        if (data.Length < 5)
        {
            throw new VoicesBuilderException("Incomplete note");
        }
        return new Note(
            GetNoteType(data[0]),
            ConvertToInt32(data[1]),
            ConvertToInt32(data[2]),
            ConvertToInt32(data[3]),
            data[4]
        );
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
        catch (FormatException e)
        {
            throw new VoicesBuilderException("Could not convert " + s + " to an int. Reason: " + e.Message);
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

// this should be internal but tests become impossible
public class MutableVoice
{
    private readonly List<Sentence> sentences = new List<Sentence>();

    public void Add(Sentence sentence)
    {
        if (sentences.Count > 0)
        {
            Sentence lastSentence = sentences[sentences.Count - 1];
            if (lastSentence.EndBeat > sentence.StartBeat)
            {
                Debug.LogWarning($"Sentence starts before previous sentence is over. Skipping this sentence."
                + $" (last ended on beat {lastSentence.EndBeat}, this should start on beat {sentence.StartBeat})");
            }
            else if (lastSentence.LinebreakBeat > sentence.StartBeat)
            {
                throw new VoicesBuilderException("Sentence conflicts with linebreak of previous sentence");
            }
            else
            {
                sentences.Add(sentence);
            }
        }
        else
        {
            sentences.Add(sentence);
        }
    }

    // this needs to be switched over to IReadOnlyList
    public List<Sentence> GetSentences()
    {
        return sentences;
    }

    public static explicit operator Voice(MutableVoice mv)
    {
        if (mv == null)
        {
            throw new ArgumentNullException("mv");
        }
        return new Voice(mv.GetSentences());
    }
}

// this should be internal but tests become impossible
public class MutableSentence
{
    private readonly List<Note> notes = new List<Note>();
    private int linebreakBeat;

    public void Add(Note note)
    {
        if (note == null)
        {
            throw new ArgumentNullException("note");
        }
        if (linebreakBeat != 0)
        {
            throw new VoicesBuilderException("Adding more notes after the linebreak has already been set is not allowed");
        }
        else if (GetUntilBeat() > note.StartBeat)
        {
            throw new VoicesBuilderException("New note overlaps with existing sentence");
        }
        else
        {
            notes.Add(note);
        }
    }

    private int GetUntilBeat()
    {
        if (notes.Count == 0)
        {
            return 0;
        }
        Note lastNote = notes[notes.Count - 1];
        return lastNote.StartBeat + lastNote.Length;
    }

    // this needs to be switched over to IReadOnlyList
    public List<Note> GetNotes()
    {
        return notes;
    }

    public void SetLinebreakBeat(int beat)
    {
        int untilBeat = GetUntilBeat();
        if (beat < untilBeat)
        {
            Debug.LogWarning($"Linebreak on beat {beat} conflicts with existing sentence. Using the beat {untilBeat + 1} instead");
            beat = untilBeat + 1;
        }
        linebreakBeat = beat;
    }

    public int GetLinebreakBeat()
    {
        return linebreakBeat;
    }

    public static explicit operator Sentence(MutableSentence ms)
    {
        if (ms == null)
        {
            throw new ArgumentNullException("ms");
        }
        return new Sentence(ms.GetNotes(), ms.GetLinebreakBeat());
    }
}
