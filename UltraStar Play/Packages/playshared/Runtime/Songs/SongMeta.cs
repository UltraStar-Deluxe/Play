using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

[Serializable]
public class SongMeta
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
     * MusicBrainz identifier for the Recording.
     * See https://musicbrainz.org/doc/MusicBrainz_Identifier
     */
    public virtual string MusicBrainzRecord { get; set; } = "";

    /**
     * MusicBrainz identifier for the Release.
     * See https://musicbrainz.org/doc/MusicBrainz_Identifier
     */
    public virtual string MusicBrainzRelease { get; set; } = "";

    /**
     * MusicBrainz identifier for the Release Group.
     * See https://musicbrainz.org/doc/MusicBrainz_Identifier
     */
    public virtual string MusicBrainzReleaseGroup { get; set; } = "";

    /**
     * MusicBrainz identifier for the artist.
     * See https://musicbrainz.org/doc/MusicBrainz_Identifier
     */
    public virtual string MusicBrainzArtist { get; set; } = "";

    /**
     * Artist of the song.
     */
    public virtual string Artist { get; set; } = "";

    /**
     * Title of the song.
     */
    public virtual string Title { get; set; } = "";

    /**
     * Beats per minute of the audio.
     */
    public virtual float BeatsPerMinute { get; set; }

    /**
     * Path or URI to the audio file.
     */
    public virtual string Mp3 { get; set; } = "";

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
     * URI to load the song in the embedded WebView.
     */
    public virtual string Website { get; set; } = "";

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

    /**
     * Mapping from voice IDs ("P1", "P2", "P3", ...)
     * to performer names ("Elvis Presley", "Shakira")
     */
    protected readonly Dictionary<EVoiceId, string> voiceIdToDisplayName = new();

    /**
     * Mapping from voice IDs ("P1", "P2", "P3", ...)
     * to the voice data structure.
     */
    protected readonly Dictionary<EVoiceId, Voice> voiceIdToVoice = new();
    public virtual IReadOnlyCollection<Voice> Voices => voiceIdToVoice.Values;

    /**
     * Number of available voices.
     */
    public virtual int VoiceCount => voiceIdToVoice.Count;

    /**
     * Values that does not have a dedicated field in
     * this data structure can be added here.
     */
    private readonly Dictionary<string, string> additionalHeaderEntries = new();
    public IReadOnlyDictionary<string, string> AdditionalHeaderEntries
    {
        get
        {
            return additionalHeaderEntries;
        }
    }

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
        BeatsPerMinute = other.BeatsPerMinute;
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

    public virtual bool TryGetVoice(EVoiceId voiceId, out Voice voice)
    {
        return voiceIdToVoice.TryGetValue(voiceId, out voice);
    }

    public virtual void AddVoice(Voice voice)
    {
        if (voice == null)
        {
            return;
        }

        voiceIdToVoice[voice.Id] = voice;
    }

    public virtual void RemoveVoice(EVoiceId voiceId)
    {
        voiceIdToVoice.Remove(voiceId);
    }
}
