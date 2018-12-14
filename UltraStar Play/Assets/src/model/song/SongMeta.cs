using System.Collections.Generic;
using System.Text;
using System;
using System.IO;

public class SongMeta
{
    // required helper fields
    public string Directory {get;}
    public string Filename {get;}

    // required fields
    public string Artist {get;}
    public float Bpm {get;}
    public string Cover {get;}
    public string Mp3 {get;}
    public string Title {get;}

    // required special fields
    public Dictionary<string, string> VoiceNames {get;}
    public Encoding Encoding {get;}

    // optional fields
    public string Background {get; set;}
    public string Edition {get; set;}
    public float End {get; set;}
    public float Gap {get; set;}
    public string Genre {get; set;}
    public string Language {get; set;}
    public bool Relative {get; set;}// = false; // setting default values here does not work in C# 4.0
    public float Start {get; set;}// = 0; // setting default values here does not work in C# 4.0
    public string Video {get; set;}
    public float VideoGap {get; set;}
    public uint Year {get; set;}

    public SongMeta(
        // required helper fields
        string directory,
        string filename,
        // required fields
        string artist,
        float bpm,
        string cover,
        string mp3,
        string title,
        // required special fields
        Dictionary<string, string> voiceNames,
        Encoding encoding
    )
    {
        // C# 4.0 does not support the 'nameof' keyword, hence the strings
        if (directory == null) throw new ArgumentNullException("directory");
        if (filename == null) throw new ArgumentNullException("filename");
        if (artist == null) throw new ArgumentNullException("artist");
        if (cover == null) throw new ArgumentNullException("cover");
        if (mp3 == null) throw new ArgumentNullException("mp3");
        if (title == null) throw new ArgumentNullException("title");
        if (voiceNames == null) throw new ArgumentNullException("voiceNames");
        if (encoding == null) throw new ArgumentNullException("encoding");

        Directory = directory;
        Filename = filename;

        Artist = artist;
        Bpm = bpm;
        Cover = cover;
        Mp3 = Mp3;
        Title = title;

        VoiceNames = voiceNames;
        Encoding = encoding;

        // set some defaults that we could not set otherwise
        Gap = 0;
        Relative = false;
        Start = 0;
    }

    public Dictionary<string, Voice> GetVoices()
    {
        return VoicesBuilder.ParseFile(Directory + Path.PathSeparator + Filename, Encoding, VoiceNames.Keys);
    }
}
