using System;
using System.Collections.Generic;
using UniRx;

public class UltraStarSongMeta : SongMeta
{
    public bool HasFailedToLoadVoices { get; private set; }
    private bool hasLoadedVoices;

    private bool ShouldLoadVoices => !hasLoadedVoices && !HasFailedToLoadVoices;

    /**
     * The "bars-per-minute" in four-four-time (i.e. (beats-per-minute / 4)) of the song.
     * Example: a BPM value of 60 in a txt file would define a beat every 0.25 seconds (60*4=240 beats-per-minute).
     */
    public float TxtFileBpm { get; private set; }
    public override float BeatsPerMinute {
        get
        {
            // UltraStar BPM is not "beats per minute" but "bars per minute" in four-four-time.
            // To get the common "beats per minute", one has to multiply with 4.
            return TxtFileBpm * 4;
        }
        set
        {
            TxtFileBpm = value / 4;
        }
    }

    public override int VoiceCount => !voiceIdToDisplayName.IsNullOrEmpty()
        ? voiceIdToDisplayName.Count
        : Voices.Count;

    public override string GetVoiceDisplayName(EVoiceId voiceId)
    {
        if (voiceIdToDisplayName.IsNullOrEmpty()
            && ShouldLoadVoices)
        {
            LoadVoicesFromFile();
        }
        return base.GetVoiceDisplayName(voiceId);
    }

    public override IReadOnlyCollection<Voice> Voices
    {
        get
        {
            if (ShouldLoadVoices)
            {
                LoadVoicesFromFile();
            }

            return base.Voices;
        }
    }

    public override bool TryGetVoice(EVoiceId voiceId, out Voice voice)
    {
        if (ShouldLoadVoices)
        {
            LoadVoicesFromFile();
        }

        return base.TryGetVoice(voiceId, out voice);
    }

    private readonly Subject<bool> loadedVoicesEventStream = new();
    public IObservable<bool> LoadedVoicesEventStream => loadedVoicesEventStream;

    public UltraStarSongMeta(SongMeta other)
    {
        CopyValues(other);
    }

    public UltraStarSongMeta(
        string artist,
        string title,
        float txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
    {
        Artist = artist ?? throw new ArgumentNullException(nameof(artist));
        TxtFileBpm = txtFileBpm;
        Audio = audioFile ?? throw new ArgumentNullException(nameof(audioFile));
        Title = title ?? throw new ArgumentNullException(nameof(title));

        if (voiceIdToDisplayName == null)
        {
            throw new ArgumentNullException(nameof(voiceIdToDisplayName));
        }
        this.voiceIdToDisplayName.AddRange(voiceIdToDisplayName);
    }

    public override void AddVoice(Voice voice)
    {
        base.AddVoice(voice);

        // No need to load the voices anymore.
        hasLoadedVoices = true;
    }

    private void LoadVoicesFromFile()
    {
        if (HasFailedToLoadVoices)
        {
            return;
        }

        // Do not attempt to load voices again.
        hasLoadedVoices = true;

        // This field is not reset if any errors occurred.
        HasFailedToLoadVoices = true;

        if (FileInfo == null
            || !FileInfo.Exists)
        {
            return;
        }

        bool.TryParse(GetAdditionalHeaderEntry("relative"), out bool isRelativeSongFormat);

        List<Voice> voices = UltraStarSongVoicesParser.ParseFile(
            FileInfo.FullName,
            FileEncoding,
            isRelativeSongFormat,
            false);
        voices.ForEach(voice => AddVoice(voice));

        // All done without errors.
        HasFailedToLoadVoices = false;
    }

    // public override void CopyValues(SongMeta other)
    // {
    //     base.CopyValues(other);
    //     TxtFileBpm = other.BeatsPerMinute / 4;
    // }
}
