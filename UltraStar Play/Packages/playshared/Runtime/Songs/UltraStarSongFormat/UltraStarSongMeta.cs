using System;
using System.Collections.Generic;
using UniRx;

public class UltraStarSongMeta : SongMeta
{
    public bool HasFailedToLoadVoices { get; private set; }
    private bool hasLoadedVoices;

    private bool ShouldLoadVoices => !hasLoadedVoices && !HasFailedToLoadVoices;

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

    public UltraStarSongMeta(
        string artist,
        string title,
        float bpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
    {
        Artist = artist ?? throw new ArgumentNullException(nameof(artist));
        Bpm = bpm;
        Mp3 = audioFile ?? throw new ArgumentNullException(nameof(audioFile));
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
}
