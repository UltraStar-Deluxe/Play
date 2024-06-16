using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SongQueueManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongQueueManager>();

    private readonly List<SongQueueEntryDto> songQueueEntryDtos = new();

    public int SongQueueLength => songQueueEntryDtos.Count;
    public bool IsSongQueueEmpty => songQueueEntryDtos.IsNullOrEmpty();

    private readonly Subject<SongQueueChangedEvent> songQueueChangedEventStream = new();
    public IObservable<SongQueueChangedEvent> SongQueueChangedEventStream => songQueueChangedEventStream;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    protected override object GetInstance()
    {
        return Instance;
    }

    public void AddSongQueueEntry(SongQueueEntryDto songQueueEntryDto)
    {
        songQueueEntryDtos.Add(songQueueEntryDto);
        songQueueChangedEventStream.OnNext(new SongQueueChangedEvent(songQueueEntryDto));
    }

    public void RemoveSongQueueEntry(SongQueueEntryDto songQueueEntryDto)
    {
        songQueueEntryDtos.Remove(songQueueEntryDto);
        songQueueChangedEventStream.OnNext(new SongQueueChangedEvent(songQueueEntryDto));
    }

    public void RemoveSongQueueEntries(List<SongQueueEntryDto> entries)
    {
        songQueueEntryDtos.RemoveAll(entries);
        songQueueChangedEventStream.OnNext(new SongQueueChangedEvent(entries));
    }

    public void ToggleMedley(SongQueueEntryDto songQueueEntryDto)
    {
        songQueueEntryDto.IsMedleyWithPreviousEntry = !songQueueEntryDto.IsMedleyWithPreviousEntry;
        songQueueChangedEventStream.OnNext(new SongQueueChangedEvent(songQueueEntryDto));
    }

    public IReadOnlyList<SongQueueEntryDto> GetSongQueueEntries()
    {
        return songQueueEntryDtos;
    }

    public SingSceneData CreateNextSingSceneData(PartyModeSceneData partyModeSceneData)
    {
        List<SongQueueEntryDto> nextEntries = PeekNextSongQueueEntries();
        if (nextEntries.IsNullOrEmpty())
        {
            return null;
        }

        RemoveSongQueueEntries(nextEntries);

        List<SongMeta> songMetas = nextEntries
            .Select(entry => songMetaManager.GetSongMetaByLocallyUniqueId(entry.SongDto.Hash))
            .ToList();

        SongQueueEntryDto firstEntry = nextEntries.FirstOrDefault();

        SingSceneData singSceneData = new();
        singSceneData.SongMetas = songMetas;
        singSceneData.SingScenePlayerData = DtoConverter.FromDto(firstEntry.SingScenePlayerDataDto, settings, nonPersistentSettings);
        singSceneData.gameRoundSettings = DtoConverter.FromDto(firstEntry.GameRoundSettingsDto);
        singSceneData.partyModeSceneData = partyModeSceneData;
        if (nextEntries.Count > 1)
        {
            // This is a medley
            singSceneData.MedleySongIndex = 0;
        }
        else if (singSceneData.gameRoundSettings != null
                 && singSceneData.gameRoundSettings.modifiers.AnyMatch(modifier => modifier is ShortSongGameRoundModifier))
        {
            // Set as medley song to play shortened version
            singSceneData.MedleySongIndex = 0;
        }

        return singSceneData;
    }

    public List<SongQueueEntryDto> PeekNextSongQueueEntries()
    {
        if (IsSongQueueEmpty)
        {
            return null;
        }

        List<SongQueueEntryDto> result = new();
        foreach (SongQueueEntryDto songQueueEntryDto in GetSongQueueEntries())
        {
            if (result.IsNullOrEmpty()
                || songQueueEntryDto.IsMedleyWithPreviousEntry)
            {
                result.Add(songQueueEntryDto);
            }
        }

        return result;
    }

    public string GetSongQueueEntryErrorMessage(SongQueueEntryDto songQueueEntryDto)
    {
        if (songQueueEntryDto == null)
        {
            return "Missing song queue entry";
        }

        if (songQueueEntryDto.SingScenePlayerDataDto == null)
        {
            return "Missing player data";
        }

        if (songQueueEntryDto.SingScenePlayerDataDto.PlayerProfileNames.IsNullOrEmpty())
        {
            return "Missing player profiles";
        }

        if (songQueueEntryDto.SongDto == null
            || songQueueEntryDto.SongDto.Hash.IsNullOrEmpty())
        {
            return "Missing songs";
        }

        return "";
    }

    public void UpdateSongQueueEntry(SongQueueEntryDto oldSongQueueEntryDto, SongQueueEntryDto newSongQueueEntryDto)
    {
        if (oldSongQueueEntryDto == newSongQueueEntryDto)
        {
            return;
        }

        oldSongQueueEntryDto.SongDto = newSongQueueEntryDto.SongDto;
        oldSongQueueEntryDto.SingScenePlayerDataDto = newSongQueueEntryDto.SingScenePlayerDataDto;
        oldSongQueueEntryDto.GameRoundSettingsDto = newSongQueueEntryDto.GameRoundSettingsDto;
        oldSongQueueEntryDto.IsMedleyWithPreviousEntry = newSongQueueEntryDto.IsMedleyWithPreviousEntry;

        songQueueChangedEventStream.OnNext(new (oldSongQueueEntryDto));
    }
}
