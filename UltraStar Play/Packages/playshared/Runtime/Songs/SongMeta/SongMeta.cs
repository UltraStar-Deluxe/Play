using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
     * The UltraStar format version that was used when loading the song.
     */
    public virtual UltraStarSongFormatVersion Version { get; set; }

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
     * URL to an audio resource.
     * Intended as fallback, e.g., when the regular property points to a local file.
     */
    public string AudioUrl { get; set; } = "";

    /**
     * Path or URI to the audio file that contains only the voice of the singers.
     * This audio file can be created from the source audio file using AI.
     */
    public virtual string VocalsAudio { get; set; } = "";

    /**
     * URL to an audio resource.
     * Intended as fallback, e.g., when the regular property points to a local file.
     */
    public string VocalsAudioUrl { get; set; } = "";

    /**
     * Path or URI to the audio file that contains only the instruments and no singing.
     * This audio file can be created from the source audio file using AI.
     */
    public virtual string InstrumentalAudio { get; set; } = "";

    /**
     * URL to an audio resource.
     * Intended as fallback, e.g., when the regular property points to a local file.
     */
    public string InstrumentalAudioUrl { get; set; } = "";

    /**
     * Path or URI to an image file that should be displayed as background when singing.
     */
    public virtual string Background { get; set; } = "";

    /**
     * URL to a background image resource.
     * Intended as fallback, e.g., when the regular property points to a local file.
     */
    public string BackgroundUrl { get; set; } = "";

    /**
     * Path or URI to an image file that should be displayed as preview in song selection.
     */
    public virtual string Cover { get; set; } = "";

    /**
     * URL to a cover image resource.
     * Intended as fallback, e.g., when the regular property points to a local file.
     */
    public string CoverUrl { get; set; } = "";

    /**
     * Editions of the song.
     * Multiple values can be separated by comma.
     * This is typically the name of the game or the TV show it was featured in.
     */
    public virtual string Edition { get; set; } = "";

    /**
     * Genres of the song.
     * Multiple values can be separated by comma.
     */
    public virtual string Genre { get; set; } = "";

    /**
     * The languages of the lyrics.
     * Multiple values can be separated by comma.
     */
    public virtual string Language { get; set; } = "";

    /**
     * User defined tags for the song.
     * Multiple values can be separated by comma.
     */
    public virtual string Tag { get; set; } = "";

    /**
     * Path or URI to a background video.
     */
    public virtual string Video { get; set; } = "";

    /**
     * URL to a video resource.
     * Intended as fallback, e.g., when the regular property points to a local file.
     */
    public string VideoUrl { get; set; } = "";

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
     * Identifies the remote source of a song,
     * for example the online server where it was found.
     */
    public virtual string RemoteSource { get; set; } = "";

    /**
     * Any value that does not have a dedicated field in
     * this data structure can be stored in this map.
     */
    private readonly Dictionary<string, string> additionalHeaderEntries = new();
    public virtual IReadOnlyDictionary<string, string> AdditionalHeaderEntries
    {
        get
        {
            return additionalHeaderEntries;
        }
    }

    public virtual void SetAdditionalHeaderEntry(string key, string value)
    {
        additionalHeaderEntries[key.ToLowerInvariant()] = value.OrIfNull("");
    }

    public virtual string GetAdditionalHeaderEntry(string key)
    {
        return additionalHeaderEntries.TryGetValue(key.ToLowerInvariant(), out string value)
            ? value
            : "";
    }

    public virtual void SetFileInfo(FileInfo filePath, Encoding encoding = null)
    {
        FileInfo = filePath;
        FileEncoding = encoding;
    }

    public virtual void SetFileInfo(string filePath, Encoding encoding = null)
    {
        SetFileInfo(new FileInfo(filePath), encoding);
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
        return $"'{Artist} - {Title}'";
    }

    public virtual void CopyValues(SongMeta other)
    {
        Artist = other.Artist;
        Audio = other.Audio;
        AudioUrl = other.AudioUrl;
        Background = other.Background;
        BackgroundUrl = other.BackgroundUrl;
        BeatsPerMinute = other.BeatsPerMinute;
        Cover = other.Cover;
        CoverUrl = other.CoverUrl;
        Edition = other.Edition;
        EndInMillis = other.EndInMillis;
        FileEncoding = other.FileEncoding;
        FileInfo = other.FileInfo;
        GapInMillis = other.GapInMillis;
        Genre = other.Genre;
        Tag = other.Tag;
        InstrumentalAudio = other.InstrumentalAudio;
        InstrumentalAudioUrl = other.InstrumentalAudioUrl;
        Language = other.Language;
        MedleyEndInMillis = other.MedleyEndInMillis;
        MedleyStartInMillis = other.MedleyStartInMillis;
        PreviewEndInMillis = other.PreviewEndInMillis;
        PreviewStartInMillis = other.PreviewStartInMillis;
        StartInMillis = other.StartInMillis;
        Title = other.Title;
        Video = other.Video;
        VideoGapInMillis = other.VideoGapInMillis;
        VideoUrl = other.VideoUrl;
        VocalsAudio = other.VocalsAudio;
        VocalsAudioUrl = other.VocalsAudioUrl;
        Year = other.Year;
        CopyVoices(other);
        CopyAdditionalHeaderEntries(other);
    }

    private void CopyAdditionalHeaderEntries(SongMeta other)
    {
        additionalHeaderEntries.Clear();
        other.additionalHeaderEntries.ForEach(entry =>
        {
            additionalHeaderEntries.Add(entry.Key, entry.Value);
        });
    }

    private void CopyVoices(SongMeta other)
    {
        voiceIdToVoice.Clear();
        voiceIdToDisplayName.Clear();
        if (other.VoiceCount <= 0)
        {
            return;
        }

        other.Voices.ForEach(voice =>
        {
            Voice voiceClone = voice.CloneDeep();
            AddVoice(voiceClone);
        });

        other.voiceIdToDisplayName.ForEach(entry =>
        {
            voiceIdToDisplayName.Add(entry.Key, entry.Value);
        });
    }
}
