using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

static class SongMetaBuilder
{
    public static SongMeta ParseFile(string path, Encoding enc = null)
    {
        using (StreamReader reader = TxtReader.GetFileStreamReader(path, enc))
        {
            bool finishedHeaders = false;
            string directory = new FileInfo(path).Directory.FullName;
            string filename = new FileInfo(path).Name;

            Dictionary<string, string> requiredFields = new Dictionary<string, string>{
                {"artist", null},
                {"bpm", null},
                {"mp3", null},
                {"title", null}
            };
            Dictionary<string, string> voiceNames = new Dictionary<string, string>();
            Dictionary<string, string> otherFields = new Dictionary<string, string>();

            uint lineNumber = 0;
            while (!finishedHeaders && !reader.EndOfStream)
            {
                ++lineNumber;
                string line = reader.ReadLine();
                if (!line.StartsWith("#", StringComparison.Ordinal))
                {
                    if (lineNumber == 1)
                    {
                        throw new SongMetaBuilderException(path + " does not look like a song file; ignoring");
                    }
                    finishedHeaders = true;
                    break;
                }
                char[] separator = { ':' };
                string[] parts = line.Substring(1).Split(separator, 2);
                if (parts.Length < 2 || parts[0].Length < 1 || parts[1].Length < 1)
                {
                    Debug.LogWarning("Invalid line formatting on line " + line + " of file " + path);
                    // Ignore this line. Continue with the next line.
                    continue;
                }
                string tag = parts[0].ToLowerInvariant();
                string val = parts[1];

                if (tag.Equals("encoding", StringComparison.Ordinal))
                {
                    if (val.Equals("UTF8", StringComparison.Ordinal))
                    {
                        val = "UTF-8";
                    }
                    Encoding newEncoding = Encoding.GetEncoding(val);
                    if (!newEncoding.Equals(reader.CurrentEncoding))
                    {
                        reader.Dispose();
                        return ParseFile(path, newEncoding);
                    }
                }
                else if (requiredFields.ContainsKey(tag))
                {
                    requiredFields[tag] = val;
                }
                else if (tag.Equals("previewstart"))
                {
                    otherFields[tag] = val;
                }
                else if (tag.StartsWith("previewend"))
                {
                    otherFields[tag] = val;
                }
                else if (tag.StartsWith("p", StringComparison.Ordinal)
                    && tag.Length == 2
                    && Char.IsDigit(tag, 1))
                {
                    if (!voiceNames.ContainsKey(tag.ToUpperInvariant()))
                    {
                        voiceNames.Add(tag.ToUpperInvariant(), val);
                    }
                    // silently ignore already set voiceNames
                }
                else if (tag.StartsWith("duetsingerp", StringComparison.Ordinal)
                    && tag.Length == 12
                    && Char.IsDigit(tag, 11))
                {
                    string shorttag = tag.Substring(10).ToUpperInvariant();
                    if (!voiceNames.ContainsKey(shorttag))
                    {
                        voiceNames.Add(shorttag, val);
                    }
                    // silently ignore already set voiceNames
                }
                else
                {
                    if (otherFields.ContainsKey(tag))
                    {
                        throw new SongMetaBuilderException("Cannot set '" + tag + "' twice in file " + path);
                    }
                    otherFields.Add(tag, val);
                }
            }

            // this _should_ get handled by the ArgumentNullException
            // further down below, but that produces really vague
            // messages about a parameter 's' for some reason
            foreach (var item in requiredFields)
            {
                if (item.Value == null)
                {
                    throw new SongMetaBuilderException("Required tag '" + item.Key + "' was not set in file: " + path);
                }
            }

            //Read the song file body
            StringBuilder songBody = new StringBuilder();
            string bodyLine;
            while ((bodyLine = reader.ReadLine()) != null)
            {
                songBody.Append(bodyLine); //Ignorning the newlines for the hash
            }

            //Hash the song file body
            string songHash = Hashing.Md5(Encoding.UTF8.GetBytes(songBody.ToString()));
            Debug.Log("Hash for " + requiredFields["title"] + " is " + songHash);


            try
            {
                SongMeta res = new SongMeta(
                    directory,
                    filename,
                    songHash,
                    requiredFields["artist"],
                    ConvertToFloat(requiredFields["bpm"]),
                    requiredFields["mp3"],
                    requiredFields["title"],
                    voiceNames,
                    reader.CurrentEncoding
                );
                foreach (var item in otherFields)
                {
                    switch (item.Key)
                    {
                        case "background":
                            res.Background = item.Value;
                            break;
                        case "cover":
                            res.Cover = item.Value;
                            break;
                        case "edition":
                            res.Edition = item.Value;
                            break;
                        case "end":
                            res.End = ConvertToFloat(item.Value);
                            break;
                        case "gap":
                            res.Gap = ConvertToFloat(item.Value);
                            break;
                        case "genre":
                            res.Genre = item.Value;
                            break;
                        case "language":
                            res.Language = item.Value;
                            break;
                        case "start":
                            res.Start = ConvertToFloat(item.Value);
                            break;
                        case "video":
                            res.Video = item.Value;
                            break;
                        case "videogap":
                            res.VideoGap = ConvertToFloat(item.Value);
                            break;
                        case "year":
                            res.Year = ConvertToUInt32(item.Value);
                            break;
                        default:
                            res.AddUnkownHeaderEntry(item.Key, item.Value);
                            break;
                    }
                }
                return res;
            }
            catch (ArgumentNullException e)
            {
                // if you get these with e.ParamName == "s", it's probably one of the non-nullable things (ie, float, uint, etc)
                throw new SongMetaBuilderException("Required tag '" + e.ParamName + "' was not set in file: " + path);
            }
        }
    }

    private static float ConvertToFloat(string s)
    {
        // Some txt files use comma as decimal separator (e.g. "12,34" instead "12.34").
        // Convert this to English notation.
        string sWithDotAsDecimalSeparator = s.Replace(",", ".");
        if (float.TryParse(sWithDotAsDecimalSeparator, NumberStyles.Any, CultureInfo.InvariantCulture, out float res))
        {
            return res;
        }
        else
        {
            throw new SongMetaBuilderException("Could not convert " + s + " to a float.");
        }
    }

    private static uint ConvertToUInt32(string s)
    {
        try
        {
            return Convert.ToUInt32(s, 10);
        }
        catch (FormatException e)
        {
            throw new SongMetaBuilderException("Could not convert " + s + " to an uint. Reason: " + e.Message);
        }
    }
}

[Serializable]
public class SongMetaBuilderException : Exception
{
    public SongMetaBuilderException()
    {
    }

    public SongMetaBuilderException(string message)
        : base(message)
    {
    }

    public SongMetaBuilderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
