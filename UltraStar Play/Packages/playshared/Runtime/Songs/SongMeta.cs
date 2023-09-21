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
     * Artist of the song.
     */
    public virtual string Artist { get; set; } = "";

    /**
     * Title of the song.
     */
    public virtual string Title { get; set; } = "";

    /**
     * Year in which the song was released.
     */
    public virtual uint Year { get; set; }

    /**
     * Path or URI to an audio or video file (i.e. when using a video as audio source).
     */
    public virtual string Audio { get; set; } = "";

    /**
     * Path or URI to the audio file that contains only the voice of the singers.
     * This audio file can be created from the source audio file using AI.
     */
    public virtual string VocalsAudio { get; set; } = "";

    /**
     * Path or URI to the audio file that contains only the instruments and no singing.
     * This audio file can be created from the source audio file using AI.
     */
    public virtual string InstrumentalAudio { get; set; } = "";

    /**
     * URI to load the song in the embedded WebView.
     */
    public virtual string Website { get; set; } = "";

    /**
     * Path or URI to an image file that should be displayed as background when singing.
     */
    public virtual string Background { get; set; } = "";

    /**
     * Path or URI to an image file that should be displayed as preview in song selection.
     */
    public virtual string Cover { get; set; } = "";

    /**
     * Edition of the song.
     * This is typically the name of the game or the TV show it was featured in.
     */
    public virtual string Edition { get; set; } = "";

    /**
     * Genre of the song.
     */
    public virtual string Genre { get; set; } = "";

    /**
     * The language of the lyrics.
     */
    public virtual string Language { get; set; } = "";

    /**
     * Path or URI to a background video.
     */
    public virtual string Video { get; set; } = "";

    /**
     * Beats per minute of the audio.
     * This defines the grid for positioning note.
     * Further, pitch detection is done per beat when singing.
     * Thus, changing the BPM value can impact the singing score.
     */
    public virtual double BeatsPerMinute { get; set; }

    /**
     * The time to first lyrics in millisecond.
     * More specifically, the time until the beat position 0 is reached.
     * In a well done UltraStar song, the first note starts at beat 0
     * and the song uses a corresponding GAP.
     */
    public virtual double GapInMillis { get; set; }

    /**
     * Delay in milliseconds for the video playback relative to the audio.
     * A positive value will skip this part of the video.
     * A negative value will wait before playing the video.
     */
    public virtual double VideoGapInMillis { get; set; }

    /**
     * Time in milliseconds at which the preview of the song should begin.
     */
    public virtual double PreviewStartInMillis { get; set; }

    /**
     * Time in milliseconds (or beat?) at which the preview should end.
     * Not implemented.
     */
    public virtual double PreviewEndInMillis { get; set; }

    /**
     * Start in milliseconds to skip the beginning of the audio.
     */
    public virtual double StartInMillis { get; set; }

    /**
     * End in milliseconds to skip the ending of the audio.
     */
    public virtual double EndInMillis { get; set; }

    /**
     * Time in milliseconds where the singing should begin when playing a medley.
     * A countdown is shown before this time
     * (i.e. the audio is started before this time, but scoring starts here).
     */
    public virtual double MedleyStartInMillis { get; set; }

    /**
     * Time in milliseconds where the singing should end when playing a medley.
     * Afterwards, the next medley song will be started.
     */
    public virtual double MedleyEndInMillis { get; set; }

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
     * Any value that does not have a dedicated field in
     * this data structure can be stored in this map.
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

    public virtual void SetFileInfo(string filePath, Encoding encoding = null)
    {
        FileInfo = new FileInfo(filePath);
        FileEncoding = encoding;
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

    public override string ToString()
    {
        return $"{nameof(SongMeta)}(artist: '{Artist}', title: '{Title}', file: '{FileInfo}')";
    }

    public virtual void CopyValues(SongMeta other)
    {
        FileEncoding = other.FileEncoding;

        Artist = other.Artist;
        Title = other.Title;
        BeatsPerMinute = other.BeatsPerMinute;
        Audio = other.Audio;
        VocalsAudio = other.VocalsAudio;
        InstrumentalAudio = other.InstrumentalAudio;

        Background = other.Background;
        Cover = other.Cover;
        Edition = other.Edition;
        EndInMillis = other.EndInMillis;
        GapInMillis = other.GapInMillis;
        Genre = other.Genre;
        Language = other.Language;
        StartInMillis = other.StartInMillis;
        PreviewStartInMillis = other.PreviewStartInMillis;
        PreviewEndInMillis = other.PreviewEndInMillis;
        Video = other.Video;
        VideoGapInMillis = other.VideoGapInMillis;
        Year = other.Year;
    }
}
