using System.Collections.Generic;
using System.Linq;

public static class DtoConverter
{
    public static SongDto ToDto(SongMeta songMeta)
    {
        SongDto dto = new()
        {
            Artist = songMeta.Artist,
            Title = songMeta.Title,
            Hash = SongIdManager.GetAndCacheLocallyUniqueId(songMeta),
        };
        return dto;
    }

    public static SongMeta FromDto(SongDto dto, SongMetaManager songMetaManager)
    {
        return songMetaManager.GetSongMetaByLocallyUniqueId(dto.Hash);
    }

    public static MicProfileDto ToDto(MicProfile micProfile)
    {
        MicProfileDto dto = new()
        {
            Name = micProfile.Name,
            ChannelIndex = micProfile.ChannelIndex,
            Color = micProfile.Color,
            Amplification = micProfile.Amplification,
            NoiseSuppression = micProfile.NoiseSuppression,
            IsEnabled = micProfile.IsEnabled,
            DelayInMillis = micProfile.DelayInMillis,
            SampleRate = micProfile.SampleRate,
            ConnectedClientId = micProfile.ConnectedClientId,
        };
        return dto;
    }

    public static MicProfile FromDto(MicProfileDto dto)
    {
        MicProfile micProfile = new(dto.Name, dto.ChannelIndex, dto.ConnectedClientId)
        {
            SampleRate = dto.SampleRate,
            DelayInMillis = dto.DelayInMillis,
            IsEnabled = dto.IsEnabled,
            NoiseSuppression = dto.NoiseSuppression,
            Amplification = dto.Amplification,
            Color = dto.Color,
        };
        return micProfile;
    }

    public static SingScenePlayerDataDto ToDto(SingScenePlayerData singScenePlayerData)
    {
        List<string> playerProfileNames = singScenePlayerData.SelectedPlayerProfiles
            .Select(it => it.Name)
            .ToList();

        Dictionary<string, MicProfileDto> playerProfileNameToMicProfileDto = new();
        singScenePlayerData.PlayerProfileToMicProfileMap.ForEach(entry =>
        {
            playerProfileNameToMicProfileDto[entry.Key.Name] = ToDto(entry.Value);
        });

        Dictionary<string, EExtendedVoiceId> playerProfileNameToVoiceIdMap = new();
        singScenePlayerData.PlayerProfileToVoiceIdMap.ForEach(entry =>
        {
            playerProfileNameToVoiceIdMap[entry.Key.Name] = entry.Value;
        });

        SingScenePlayerDataDto dto = new()
        {
            PlayerProfileNames = playerProfileNames,
            PlayerProfileToMicProfileMap = playerProfileNameToMicProfileDto,
            PlayerProfileToVoiceIdMap = playerProfileNameToVoiceIdMap,
        };
        return dto;
    }

    public static SingScenePlayerData FromDto(SingScenePlayerDataDto dto, Settings settings, NonPersistentSettings nonPersistentSettings)
    {
        if (dto == null)
        {
            return null;
        }

        SingScenePlayerData singScenePlayerData = new();
        singScenePlayerData.SelectedPlayerProfiles = dto.PlayerProfileNames
            .Select(playerProfileName => SettingsUtils.GetPlayerProfile(settings, nonPersistentSettings, playerProfileName))
            .Where(it => it != null)
            .ToList();
        singScenePlayerData.PlayerProfileToMicProfileMap = new();
        dto.PlayerProfileToMicProfileMap.ForEach(entry =>
        {
            PlayerProfile playerProfile = SettingsUtils.GetPlayerProfile(settings, nonPersistentSettings, entry.Key);
            MicProfile micProfile = SettingsUtils.GetMicProfile(settings, entry.Value.Name, entry.Value.ChannelIndex);
            if (playerProfile != null
                && micProfile != null)
            {
                singScenePlayerData.PlayerProfileToMicProfileMap[playerProfile] = micProfile;
            }
        });
        singScenePlayerData.PlayerProfileToVoiceIdMap = new();
        dto.PlayerProfileToVoiceIdMap.ForEach(entry =>
        {
            PlayerProfile playerProfile = SettingsUtils.GetPlayerProfile(settings, nonPersistentSettings, entry.Key);
            if (playerProfile != null)
            {
                singScenePlayerData.PlayerProfileToVoiceIdMap[playerProfile] = entry.Value;
            }
        });
        return singScenePlayerData;
    }

    public static GameRoundSettings FromDto(GameRoundSettingsDto dto)
    {
        if (dto == null)
        {
            return null;
        }

        List<IGameRoundModifier> gameRoundModifiers = DtoConverter.FromDto(dto.ModifierDtos);
        return new GameRoundSettings()
        {
            modifiers = gameRoundModifiers,
        };
    }

    private static List<IGameRoundModifier> FromDto(List<GameRoundModifierDto> dtos)
    {
        if (dtos.IsNullOrEmpty())
        {
            return new List<IGameRoundModifier>();
        }

        List<string> modifierIds = dtos
            .Select(dto => dto.Id)
            .ToList();
        return GameRoundModifierUtils.GetGameRoundModifiersById(modifierIds);
    }

    public static List<GameRoundModifierDto> ToDto(List<IGameRoundModifier> modifiers)
    {
        return modifiers
            .Select(modifier => ToDto(modifier))
            .ToList();
    }

    private static GameRoundModifierDto ToDto(IGameRoundModifier modifier)
    {
        GameRoundModifierDto dto = new()
        {
            Id = modifier.GetId(),
            DisplayName = modifier.DisplayName,
            DisplayOrder = modifier.DisplayOrder,
        };
        return dto;
    }
}
