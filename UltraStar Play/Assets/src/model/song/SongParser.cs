using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

static class SongParser
{
    public static bool ParseSongFile(string path, Encoding enc = null)
    {
        bool endFound = false; // True if end tag was found

        SongBuilder songBuilder = new SongBuilder(path);

        char[] trimChars = { ' ', ':' };
        char[] splitChars = { ' ' };

        Dictionary<ESongHeader, System.Object> headers = new Dictionary<ESongHeader, System.Object>();

        try
        {
            using (StreamReader reader = TxtReader.GetFileStreamReader(path, enc))
            {
                bool finishedHeaders = false;
                string directory = new FileInfo(path).Directory.FullName;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == "" || line[0].Equals(" ")
                        || (finishedHeaders && line[0].Equals('#')))
                    {
                        Debug.Log(String.Format("Invalid linestart found in {0} :: \"{1}\". Aborting.", path, line));
                        return false;
                    }
                    if (!finishedHeaders && line[0].Equals('#'))
                    {
                        int pos = line.IndexOf(":", StringComparison.Ordinal);
                        string identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                        if (identifier.Contains(" ") || identifier.Length < 2)
                        {
                            HandleParsingError("invalid file...", EParsingErrorSeverity.Critical);
                            continue;
                        }
                        string tag = line.Substring(pos + 1).Trim();

                        if (tag.Length == 0)
                        {
                            // invalid tag.
                            HandleParsingError("Invalid empty tag found", EParsingErrorSeverity.Minor);
                            continue;
                        }

                        if (identifier.Equals("ENCODING", StringComparison.Ordinal))
                        {
                            if (tag.Equals("UTF8", StringComparison.Ordinal))
                            {
                                tag = "UTF-8";
                            }
                            Encoding newEncoding = Encoding.GetEncoding(tag);
                            if (!newEncoding.Equals(reader.CurrentEncoding))
                            {
                                reader.Dispose();
                                return ParseSongFile(path, newEncoding);
                            }
                        }

                        identifier = ParseHeaderField(headers, directory, identifier, tag);
                        AddVoice(identifier, tag.Trim(), ref songBuilder);
                    }
                    else
                    {
                        if (!finishedHeaders)
                        {
                            finishedHeaders = true;
                            songBuilder.SetSongHeaders(headers);
                            songBuilder.FixVoices();
                        }
                        ParseLyricsTxtLine(ref endFound, trimChars, splitChars, ref line, ref songBuilder);
                    }
                }

                if (reader.EndOfStream && !finishedHeaders)
                {
                    HandleParsingError("Lyrics/Notes missing", EParsingErrorSeverity.Critical);
                }

                CheckMinimalRequiredHeaders(songBuilder.GetSongHeaders());
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error reading song file" + e.Message+"\n"+e.StackTrace);
            return false;
        }
        SongsManager.AddSongs(songBuilder.AsSong());
        return true;
    }
    
    private static void AddVoice(string identifier, string name, ref SongBuilder songBuilder)
    {
        string[] voiceIdentifiers = { "P", "DUETSINGERP" };
        foreach (string voiceIdentifier in voiceIdentifiers)
        {
            if (identifier.StartsWith(voiceIdentifier, StringComparison.Ordinal))
            {
                if (identifier.Length == voiceIdentifier.Length)
                {
                    HandleParsingError("Player identifiers cannot be empty", EParsingErrorSeverity.Critical);
                }
                songBuilder.AddVoice(identifier, name);
                break;
            }
        }
    }

    private static void CheckMinimalRequiredHeaders(Dictionary<ESongHeader, object> headers)
    {
        if (headers[ESongHeader.Title] == null)
        {
            HandleParsingError("Title tag missing", EParsingErrorSeverity.Critical);
        }

        if (headers[ESongHeader.Artist] == null)
        {
            HandleParsingError("Artist tag missing", EParsingErrorSeverity.Critical);
        }

        if (headers[ESongHeader.Mp3] == null)
        {
            HandleParsingError("MP3 tag missing", EParsingErrorSeverity.Critical);
        }

        if (headers[ESongHeader.Bpm] == null)
        {
            HandleParsingError("BPM tag missing", EParsingErrorSeverity.Critical);
        }
    }

    private static void ParseLyricsTxtLine(ref bool endFound, char[] trimChars, char[] splitChars, ref string line, ref SongBuilder songBuilder)
    {
        char tag = line[0];
        line = (line.Length >= 2 && line[1] == ' ') ? line.Substring(2) : line.Substring(1);
        uint startBeat;

        switch (tag)
        {
            case 'E':
                endFound = true;
                songBuilder.SaveCurrentSentence();
                break;
            case 'P':
                songBuilder.SaveCurrentSentence();
                songBuilder.SetCurrentVoice("P"+line.Trim(trimChars));
                break;
            case ':':
            case '*':
            case 'F':
            case 'R':
            case 'G':
                string[] noteData = line.Split(splitChars, 4);
                if (noteData.Length < 4)
                {
                    if (noteData.Length == 3)
                    {
                        HandleParsingError("Ignored note without text", EParsingErrorSeverity.Minor);
                        break;
                    }
                    HandleParsingError("Invalid note found", EParsingErrorSeverity.Critical);
                }
                int pitch;
                uint length;
                if (!uint.TryParse(noteData[0], out startBeat)
                    || !uint.TryParse(noteData[1], out length)
                    || !int.TryParse(noteData[2], out pitch))
                {
                    HandleParsingError("Invalid note found (non-numeric values)", EParsingErrorSeverity.Critical);
                    break;
                }
                string text = TxtReader.NormalizeWhiteSpaceForLoop(noteData[3]);
                if (text == "")
                {
                    HandleParsingError("Ignored note without text", EParsingErrorSeverity.Minor);
                    break;
                }

                ENoteType noteType = GetNoteType(tag);
                songBuilder.AddNote(new Note(pitch, startBeat, length, text, noteType));
                break;
            case '-':
                string[] lineBreakData = line.Split(splitChars);
                if (lineBreakData.Length < 1)
                {
                    HandleParsingError("Invalid line break found (No beat)", EParsingErrorSeverity.Critical);
                }
                if (!uint.TryParse(lineBreakData[0], out startBeat))
                {
                    HandleParsingError("Invalid line break found (Non-numeric value)", EParsingErrorSeverity.Critical);
                }

                songBuilder.SaveCurrentSentence();

                if (startBeat < 1)
                {
                    HandleParsingError("Ignored line break because position is < 1", EParsingErrorSeverity.Minor);
                }
                break;
            default:
                HandleParsingError("Unexpected or missing character (" + tag + ")", EParsingErrorSeverity.Critical);
                break;
        }
    }

    private static ENoteType GetNoteType(char c)
    {
        ENoteType res;
        switch (c)
        {
            case ':':
                res = ENoteType.Normal;
                break;
            case '*':
                res = ENoteType.Golden;
                break;
            case 'F':
                res = ENoteType.Freestyle;
                break;
            case 'R':
                res = ENoteType.Rap;
                break;
            case 'G':
                res = ENoteType.RapGolden;
                break;
            default:
                throw new SongParserException("Cannot convert '"+c.ToString()+"' to a ENoteType");
        }
        return res;
    }

    private static string ParseHeaderField(Dictionary<ESongHeader, object> headers, string directory, string identifier, string fieldValue)
    {
        switch (identifier)
        {
            case "ENCODING": // handled outside the switch. Nothing to do here
            case "UPDATED": // no in-game use, ignore to prevent unknown tag errors
            case "COMMENT": // no in-game use, ignore to prevent unknown tag errors
                break;
            case "TITLE":
                headers[ESongHeader.Title] = fieldValue.Trim();
                break;
            case "ARTIST":
                headers[ESongHeader.Artist] = fieldValue.Trim();
                break;
            case "CREATOR":
            case "AUTHOR":
            case "AUTOR":
                headers[ESongHeader.Creator] = fieldValue.Trim();
                break;
            case "MP3":
                if (File.Exists(Path.Combine(directory, fieldValue.Trim())))
                {
                    headers[ESongHeader.Mp3] = fieldValue.Trim();
                }
                else
                {
                    HandleParsingError("Can't find audio file: " + Path.Combine(directory, fieldValue), EParsingErrorSeverity.Critical);
                }
                break;
            case "BPM":
                float result;
                if (TryParse(fieldValue, out result))
                {
                    headers[ESongHeader.Bpm] = result;
                }
                else
                {
                    HandleParsingError("Invalid BPM value", EParsingErrorSeverity.Critical);
                }
                break;
            case "EDITION":
                if (fieldValue.Length > 1)
                {
                    headers[ESongHeader.Edition] = fieldValue.Trim();
                }
                else
                {
                    HandleParsingError("Invalid edition", EParsingErrorSeverity.Minor);
                }
                break;
            case "GENRE":
                if (fieldValue.Length > 1)
                {
                    headers[ESongHeader.Genre] = fieldValue.Trim();
                }
                else
                {
                    HandleParsingError("Invalid genre", EParsingErrorSeverity.Minor);
                }
                break;
            case "ALBUM":
                headers[ESongHeader.Edition] = fieldValue.Trim();
                break;
            case "YEAR":
                int num;
                if (fieldValue.Length == 4 && int.TryParse(fieldValue, out num) && num > 0)
                {
                    headers[ESongHeader.Year] = num;
                }
                else
                {
                    HandleParsingError("Invalid year", EParsingErrorSeverity.Minor);
                }
                break;
            case "LANGUAGE":
                if (fieldValue.Length > 1)
                {
                    headers[ESongHeader.Language] = fieldValue.Trim();
                }
                else
                {
                    HandleParsingError("Invalid language", EParsingErrorSeverity.Minor);
                }
                break;
            case "GAP":
                float resultGap;
                if (TryParse(fieldValue, out resultGap))
                {
                    headers[ESongHeader.Gap] = resultGap / 1000f;
                }
                else
                {
                    HandleParsingError("Invalid gap", EParsingErrorSeverity.Minor);
                }
                break;
            case "COVER":
                if (File.Exists(Path.Combine(directory, fieldValue)))
                {
                    headers[ESongHeader.Cover] = fieldValue;
                }
                else
                {
                    HandleParsingError("Can't find cover file: " + Path.Combine(directory, fieldValue), EParsingErrorSeverity.Minor);
                }
                break;
            case "BACKGROUND":
                if (File.Exists(Path.Combine(directory, fieldValue)))
                {
                    headers[ESongHeader.Background] = fieldValue;
                }
                else
                {
                    HandleParsingError("Can't find background file: " + Path.Combine(directory, fieldValue), EParsingErrorSeverity.Minor);
                }
                break;
            case "VIDEO":
                if (File.Exists(Path.Combine(directory, fieldValue)))
                {
                    headers[ESongHeader.Video] = fieldValue;
                }
                else
                {
                    HandleParsingError("Can't find video file: " + Path.Combine(directory, fieldValue), EParsingErrorSeverity.Minor);
                }
                break;
            case "VIDEOGAP":
                float resultVideoGap;
                if (TryParse(fieldValue, out resultVideoGap))
                {
                    headers[ESongHeader.Videogap] = resultVideoGap;
                }
                else
                {
                    HandleParsingError("Invalid videogap", EParsingErrorSeverity.Minor);
                }
                break;
            case "START":
                float resultStart;
                if (TryParse(fieldValue, out resultStart))
                {
                    headers[ESongHeader.Start] = resultStart;
                }
                else
                {
                    HandleParsingError("Invalid start", EParsingErrorSeverity.Critical);
                }
                break;
            case "END":
                float resultFinish;
                if (TryParse(fieldValue, out resultFinish))
                {
                    headers[ESongHeader.End] = resultFinish / 1000f;
                }
                else
                {
                    HandleParsingError("Invalid end", EParsingErrorSeverity.Critical);
                }
                break;
            default:
                if (identifier.StartsWith("DUETSINGERP", StringComparison.Ordinal)
                    || identifier.StartsWith("P", StringComparison.Ordinal))
                {
                    // do nothing, we need to distinguish between the two later on
                }
                else
                {
                    HandleParsingError("Unknown tag: #" + identifier, EParsingErrorSeverity.Minor);
                }

                break;
        }

        return identifier;
    }

    private static bool TryParse<T>(string input, out T result, bool ignoreCase = false)
            where T : struct
    {
        result = default(T);
        try
        {
            result = (T)Enum.Parse(typeof(T), input, ignoreCase);
            return true;
        }
        catch (Exception e) 
        {
            Debug.Log(e);
        }

        return false;
    }

    private static bool TryParse(string input, out float result)
    {
        string inputAsWeWant = input.Replace(',', '.');
        return Single.TryParse(inputAsWeWant, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
    }

    private static void HandleParsingError(string errorMessage, EParsingErrorSeverity errorSeverity)
    {
        switch (errorSeverity)
        {
            case EParsingErrorSeverity.Critical:
                throw new SongParserException("Critical parsing error in file.\nInner error message: " + errorMessage);
            default:
                Debug.Log(errorMessage);
                break;

        }
    }

    enum EParsingErrorSeverity
    {
        Minor,
        Critical
    }
}

[Serializable]
public class SongParserException : Exception 
{
    public SongParserException()
    {
    }

    public SongParserException(string message)
        : base(message) 
    {
    }

    public SongParserException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
