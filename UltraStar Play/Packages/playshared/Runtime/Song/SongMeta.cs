using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[Serializable]
public class SongMeta
{
    // required helper fields
    /**
     * Path of the directory of the song's txt file.
     */
    public string Directory { get; set; }
    /**
     * File name of the song's txt file (not including any directories).
     */
    public string Filename { get; set; }
    /**
     * Hash for the song's txt file, used to uniquely identify it.
     */
    public string SongHash { get; private set; }

    // required 
    /**
     * Artist of the song.
     */
    public string Artist { get; set; }
    /**
     * The "bars-per-minute" in four-four-time (i.e. (beats-per-minute / 4)) of the song.
     * Example: a BPM value of 60 in a txt file would define a beat every 0.25 seconds (60*4=240 beats-per-minute).
     */
    public float Bpm { get; set; }
    /**
     * Path to the audio file.
     */
    public string Mp3 { get; set; }
    /**
     * Title of the song.
     */
    public string Title { get; set; }

    // required special fields
    /**
     * Mapping from generic singer names ("P1", "P2", "P3", ...)
     * to custom names ("Elvis Presley", "Shakira")
     */
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
                foreach (Voice voice in GetVoices())
                {
                    voiceNames.Add(voice.Name, voice.Name);
                }
            }
            return voiceNames;
        }
    }

    /**
     * Encoding of the song's txt file.
     * Default is UTF-8.
     */
    public Encoding Encoding { get; private set; }

    // optional fields
    /**
     * Path to an image file that should be displayed as background when singing.
     */
    public string Background { get; set; }
    /**
     * Path to an image file that should be displayed as preview in song selection.
     */
    public string Cover { get; set; }
    /**
     * Edition of the song, usually either the game it was ripped from or the TV show it was featured in.
     */
    public string Edition { get; set; }
    /**
     * Shift in millisecond for the lyrics relative to the audio file.
     */
    public float Gap { get; set; }
    /**
     * Genre of the music.
     */
    public string Genre { get; set; }
    /**
     * The language of the lyrics.
     */
    public string Language { get; set; }
    /**
     * Whether the note timestamps are relative to the previous note (true) or to the start of the song (false).
     * Default is false.
     */
    public bool Relative { get; set; }
    /**
     * Beat at which a preview of the song should begin.
     * Thus, this beat should start the most memorable part of a song as a preview.
     */
    public float PreviewStart { get; set; }
    /**
     * Beat at which the preview should end.
     */
    public float PreviewEnd { get; set; }
    /**
     * The video file.
     */
    public string Video { get; set; }
    /**
     * Delay in seconds for the video playback relative to the audio file.
     */
    public float VideoGap { get; set; }
    /**
     * Year in which the song was released.
     */
    public uint Year { get; set; }

    public float Start { get; set; }
    public float End { get; set; }

    private List<Voice> voices = new();

    private readonly Dictionary<string, string> unknownHeaderEntries = new();
    public IReadOnlyDictionary<string, string> UnknownHeaderEntries
    {
        get
        {
            return unknownHeaderEntries;
        }
    }

    public SongMeta()
    {
    }

    public SongMeta(
        // required helper fields
        string directory,
        string filename,
        string songHash,
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
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));
        Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        SongHash = songHash ?? throw new ArgumentNullException(nameof(songHash));

        Artist = artist ?? throw new ArgumentNullException(nameof(artist));
        Bpm = bpm;
        Mp3 = mp3 ?? throw new ArgumentNullException(nameof(mp3));
        Title = title ?? throw new ArgumentNullException(nameof(title));

        this.voiceNames = voiceNames ?? throw new ArgumentNullException(nameof(voiceNames));
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

        // set some defaults that we could not set otherwise
        Gap = 0;
        Relative = false;
        Start = 0;
    }

    public Voice GetVoice(string voiceName)
    {
        return GetVoices().FirstOrDefault(voice => voice.VoiceNameEquals(voiceName));
    }

    public IReadOnlyList<Voice> GetVoices()
    {
        if (voices.IsNullOrEmpty())
        {
            string path = Directory + Path.DirectorySeparatorChar + Filename;
            using (new DisposableStopwatch($"Loading voices of {path} took <millis> ms"))
            {
                VoicesBuilder voicesBuilder = new(path, Encoding, Relative);
                voices = new List<Voice>(voicesBuilder.GetVoices());
            }
        }
        return voices;
    }

    public void AddVoice(Voice newVoice)
    {
        if (voices.Contains(newVoice))
        {
            return;
        }

        voices.Add(newVoice);
        voiceNames.Add(newVoice.Name, newVoice.Name);
    }

    public void RemoveVoice(Voice voice)
    {
        if (!voices.Contains(voice))
        {
            return;
        }

        voices.Remove(voice);
        voiceNames.Remove(voice.Name);
    }

    public void SetUnknownHeaderEntry(string key, string value)
    {
        unknownHeaderEntries[key] = value;
    }

    public void Reload()
    {
        SongMeta other = SongMetaBuilder.ParseFile(SongMetaUtils.GetAbsoluteSongMetaPath(this));

        // Copy values
        Encoding = other.Encoding;
        SongHash = other.SongHash;

        voiceNames = other.voiceNames;
        voices = new List<Voice>();

        Artist = other.Artist;
        Title = other.Title;
        Bpm = other.Bpm;
        Mp3 = other.Mp3;

        Background = other.Background;
        Cover = other.Cover;
        Edition = other.Edition;
        End = other.End;
        Gap = other.Gap;
        Genre = other.Genre;
        Language = other.Language;
        Relative = other.Relative;
        Start = other.Start;
        PreviewStart = other.PreviewStart;
        PreviewEnd = other.PreviewEnd;
        Video = other.Video;
        VideoGap = other.VideoGap;
        Year = other.Year;
    }
}
