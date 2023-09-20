using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniRx;

[Serializable]
public abstract class SongMeta
{
    /**
     * File name of the song's txt file (not including any directories).
     */
    public virtual FileInfo FileInfo { get; private set; }

    /**
     * Encoding of the song's txt file.
     * Default is UTF-8.
     */
    public virtual Encoding FileEncoding { get; private set; }

    /**
     * MusicBrainz identifier for the recording.
     * For example, 99 Red Balloons, the original by Nena, has a release MBID of 189002e7-3285-4e2e-92a3-7f6c30d407a2.
     * See https://musicbrainz.org/doc/Recording
     * See https://musicbrainz.org/doc/MusicBrainz_Identifier
     */
    public virtual string MusicBrainzRecord { get; set; } = "";

    /**
     * Artist of the song.
     */
    public virtual string Artist { get; set; } = "";

    /**
     * Title of the song.
     */
    public virtual string Title { get; set; } = "";

    /**
     * The "bars-per-minute" in four-four-time (i.e. (beats-per-minute / 4)) of the song.
     * Example: a BPM value of 60 in a txt file would define a beat every 0.25 seconds (60*4=240 beats-per-minute).
     */
    public virtual float Bpm { get; set; }

    /**
     * Path to the local audio file.
     */
    public virtual string Mp3 { get; set; } = "";

    /**
     * URI to load the song in the embedded WebView.
     */
    public virtual string Website { get; set; } = "";

    /**
     * Path to the audio file that contains only the voice of the singers.
     * This audio file is created from the source audio file using AI.
     */
    public virtual string VocalsAudio { get; set; } = "";

    /**
     * Path to the audio file that contains only the instruments and no singing.
     * This audio file is created from the source audio file using AI.
     */
    public virtual string InstrumentalAudio { get; set; } = "";

    /**
     * Path to an image file that should be displayed as background when singing.
     */
    public virtual string Background { get; set; } = "";

    /**
     * Path to an image file that should be displayed as preview in song selection.
     */
    public virtual string Cover { get; set; } = "";

    /**
     * Edition of the song, usually either the game it was ripped from or the TV show it was featured in.
     */
    public virtual string Edition { get; set; } = "";

    /**
     * Shift in millisecond for the lyrics relative to the audio file.
     */
    public virtual float Gap { get; set; }

    /**
     * Genre of the music.
     */
    public virtual string Genre { get; set; } = "";

    /**
     * The language of the lyrics.
     */
    public virtual string Language { get; set; } = "";

    /**
     * Time in seconds at which the preview of the song should begin.
     */
    public virtual float PreviewStart { get; set; }

    /**
     * Time in seconds (or beat?) at which the preview should end.
     * Not implemented.
     */
    public virtual float PreviewEnd { get; set; }

    /**
     * The video file.
     */
    public virtual string Video { get; set; } = "";

    /**
     * Delay in seconds for the video playback relative to the audio file.
     */
    public virtual float VideoGap { get; set; }

    /**
     * Year in which the song was released.
     */
    public virtual uint Year { get; set; }

    /**
     * Start in SECONDS to skip the beginning of the audio file.
     */
    public virtual float Start { get; set; }

    /**
     * End in MILLISECONDS to skip the ending of the audio file.
     */
    public virtual float End { get; set; }

    /**
     * First beat to sing when the song was started as medley.
     * A countdown is shown before this beat.
     */
    public virtual int MedleyStartBeat { get; set; }

    /**
     * Last beat to sing when the song was started as medley.
     * Afterwards, the next medley song will be started.
     */
    public virtual int MedleyEndBeat { get; set; }

    private readonly Dictionary<string, string> additionalHeaderEntries = new();
    public IReadOnlyDictionary<string, string> AdditionalHeaderEntries
    {
        get
        {
            return additionalHeaderEntries;
        }
    }

    /**
     * Mapping from generic voice IDs ("P1", "P2", "P3", ...)
     * to performer names ("Elvis Presley", "Shakira")
     */
    protected readonly Dictionary<EVoiceId, string> voiceIdToDisplayName = new();

    private List<Voice> voices = new();
    public IReadOnlyList<Voice> Voices
    {
        get
        {
            if (voices.IsNullOrEmpty()
                && !FailedToLoadVoices)
            {
                // When there is an Exception, then this field is not reset.
                FailedToLoadVoices = true;
                voices = LoadVoices();
                FailedToLoadVoices = false;

                loadedVoicesEventStream.OnNext(true);
            }
            return voices;
        }
    }

    public bool FailedToLoadVoices { get; private set; }
    public virtual int VoiceCount => Math.Max(1, Voices.Count);

    protected readonly Subject<bool> loadedVoicesEventStream = new();
    public IObservable<bool> LoadedVoicesEventStream => loadedVoicesEventStream;

    protected abstract List<Voice> LoadVoices();

    public virtual void SetAdditionalHeaderEntry(string key, string value)
    {
        additionalHeaderEntries[key.ToLowerInvariant()] = value;
    }

    public virtual string GetAdditionalHeaderEntry(string key)
    {
        return additionalHeaderEntries.TryGetValue(key.ToLowerInvariant(), out string value)
            ? value
            : null;
    }

    public virtual string GetVoiceDisplayName(EVoiceId voiceId)
    {
        if (voiceIdToDisplayName == null
            || !voiceIdToDisplayName.TryGetValue(voiceId, out string displayName))
        {
            return voiceId.ToString();
        }

        return displayName;
    }

    public override string ToString()
    {
        return $"{nameof(SongMeta)}(artist: '{Artist}', title: '{Title}', file: '{FileInfo}')";
    }

    public virtual void SetFileInfo(string filePath, Encoding encoding = null)
    {
        FileInfo = new FileInfo(filePath);
        FileEncoding = encoding;
    }

    public virtual void CopyValues(SongMeta other)
    {
        FileEncoding = other.FileEncoding;

        Artist = other.Artist;
        Title = other.Title;
        Bpm = other.Bpm;
        Mp3 = other.Mp3;
        VocalsAudio = other.VocalsAudio;
        InstrumentalAudio = other.InstrumentalAudio;

        Background = other.Background;
        Cover = other.Cover;
        Edition = other.Edition;
        End = other.End;
        Gap = other.Gap;
        Genre = other.Genre;
        Language = other.Language;
        Start = other.Start;
        PreviewStart = other.PreviewStart;
        PreviewEnd = other.PreviewEnd;
        Video = other.Video;
        VideoGap = other.VideoGap;
        Year = other.Year;
    }

    public virtual void AddVoice(Voice newVoice)
    {
        if (Voices.Contains(newVoice))
        {
            return;
        }

        voices.Add(newVoice);
    }

    public virtual void RemoveVoice(Voice voice)
    {
        if (!Voices.Contains(voice))
        {
            return;
        }

        voices.Remove(voice);
    }
}
