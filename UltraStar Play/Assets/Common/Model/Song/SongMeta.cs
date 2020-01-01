using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.Linq;

[Serializable]
public class SongMeta
{
    // required helper fields
    public string Directory { get; private set; }
    public string Filename { get; private set; }

    // required fields
    public string Artist { get; private set; }
    public float Bpm { get; private set; }
    public string Mp3 { get; private set; }
    public string Title { get; private set; }

    // required special fields
    private Dictionary<string, string> voiceNames;
    public Dictionary<string, string> VoiceNames
    {
        get
        {
            // If the voice names were not given in the header of the UltraStar song file,
            // then use the voice names that are found when parsing the complete file.
            if (voiceNames.IsNullOrEmpty())
            {
                voiceNames = new Dictionary<string, string>();
                foreach (KeyValuePair<string, Voice> voiceNameAndVoicePair in GetVoices())
                {
                    voiceNames.Add(voiceNameAndVoicePair.Key, voiceNameAndVoicePair.Key);
                }
            }
            return voiceNames;
        }
    }
    public Encoding Encoding { get; private set; }

    // optional fields
    public string Background { get; set; }
    public string Cover { get; set; }
    public string Edition { get; set; }
    public float End { get; set; }
    public float Gap { get; set; }
    public string Genre { get; set; }
    public string Language { get; set; }
    public bool Relative { get; set; }// = false; // setting default values here does not work in C# 4.0
    public float Start { get; set; }// = 0; // setting default values here does not work in C# 4.0
    public string Video { get; set; }
    public float VideoGap { get; set; }
    public uint Year { get; set; }

    private Dictionary<string, Voice> voices;

    private readonly Dictionary<string, string> unkownHeaderEntries = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> UnkownHeaderEntries
    {
        get
        {
            return unkownHeaderEntries;
        }
    }

    public SongMeta(
        // required helper fields
        string directory,
        string filename,
        // required fields
        string artist,
        float bpm,
        string mp3,
        string title,
        // required special fields
        Dictionary<string, string> voiceNames,
        Encoding encoding
    )
    {
        // C# 4.0 does not support the 'nameof' keyword, hence the strings
        if (directory == null)
        {
            throw new ArgumentNullException("directory");
        }
        if (filename == null)
        {
            throw new ArgumentNullException("filename");
        }
        if (artist == null)
        {
            throw new ArgumentNullException("artist");
        }
        if (mp3 == null)
        {
            throw new ArgumentNullException("mp3");
        }
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }
        if (voiceNames == null)
        {
            throw new ArgumentNullException("voiceNames");
        }
        if (encoding == null)
        {
            throw new ArgumentNullException("encoding");
        }

        Directory = directory;
        Filename = filename;

        Artist = artist;
        Bpm = bpm;
        Mp3 = mp3;
        Title = title;

        this.voiceNames = voiceNames;
        Encoding = encoding;

        // set some defaults that we could not set otherwise
        Gap = 0;
        Relative = false;
        Start = 0;
    }

    public Dictionary<string, Voice> GetVoices()
    {
        if (voices.IsNullOrEmpty())
        {
            voices = VoicesBuilder.ParseFile(Directory + Path.DirectorySeparatorChar + Filename, Encoding, voiceNames.Keys);
        }
        return voices;
    }

    public void AddUnkownHeaderEntry(string key, string value)
    {
        unkownHeaderEntries.Add(key, value);
    }
}
