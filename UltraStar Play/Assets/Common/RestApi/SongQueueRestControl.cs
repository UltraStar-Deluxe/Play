using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;
    
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;
    
    [Inject]
    private GameRoundManager gameRoundManager;
    
    [Inject]
    private SongMetaManager songMetaManager;
    
    private void Start()
    {
        httpServer.On(HttpMethod.Get, "api/rest/availablePlayers")
            .WithDescription($"Get player profiles that can be used for singing")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                List<string> playerProfileNames = settings.PlayerProfiles
                    .Where(playerProfile => playerProfile.IsSelected)
                    .Select(playerProfile => playerProfile.Name)
                    .ToList();
                requestData.Context.Response.WriteJson(playerProfileNames);
            });

        httpServer.On(HttpMethod.Get, "api/rest/availableMicrophones")
            .WithDescription($"Get microphone profiles that can be used for singing")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                List<MicProfile> enabledMicrophoneProfiles = settings.MicProfiles
                    .Where(microphoneProfile => microphoneProfile.IsEnabledAndConnected(serverSideConnectRequestManager))
                    .ToList();
                requestData.Context.Response.WriteJson(enabledMicrophoneProfiles);
            });
        
        httpServer.On(HttpMethod.Post, "api/rest/songQueue/entry")
            .WithDescription($"Add entry song queue to the song queue.")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string json = requestData.Context.Request.GetBodyAsString();
                GameRoundDataDto gameRoundDataDto = JsonConverter.FromJson<GameRoundDataDto>(json, false);
                GameRoundData gameRoundData = CreateGameRoundData(gameRoundDataDto);
                
                string errorMessage = GetGameRoundDataErrorMessage(gameRoundData);
                if (!errorMessage.IsNullOrEmpty())
                {
                    Debug.LogError($"Invalid game round data: {errorMessage}");
                    return;
                }
                
                gameRoundManager.AddGameRound(gameRoundData);
            });
    }

    private GameRoundData CreateGameRoundData(GameRoundDataDto dto)
    {
        if (dto == null)
        {
            return null;
        }
        
        GameRoundData gameRoundData = new GameRoundData();
        gameRoundData.IsMedley = dto.IsMedley;
        gameRoundData.SongMetas = dto.SongIds
            .Select(songId => songMetaManager.GetSongMetaById(songId))
            .ToList();
        gameRoundData.GameRoundSettings = new(dto.GameRoundSettings);
        gameRoundData.SingScenePlayerData = CreateSingScenePlayerData(dto.SingScenePlayerDataDto);
        return gameRoundData;
    }

    private SingScenePlayerData CreateSingScenePlayerData(SingScenePlayerDataDto dto)
    {
        if (dto == null)
        {
            return null;
        }
        
        SingScenePlayerData singScenePlayerData = new();
        singScenePlayerData.SelectedPlayerProfiles = dto.PlayerProfileNames
            .Select(playerProfileName => GetPlayerProfile(playerProfileName))
            .Where(it => it != null)
            .ToList();
        singScenePlayerData.PlayerProfileToMicProfileMap = new();
        dto.PlayerProfileToMicProfileMap.ForEach(entry =>
        {
            PlayerProfile playerProfile = GetPlayerProfile(entry.Key);
            MicProfile micProfile = GetMicProfile(entry.Value);
            if (playerProfile != null
                && micProfile != null)
            {
                singScenePlayerData.PlayerProfileToMicProfileMap[playerProfile] = micProfile;
            }
        });
        singScenePlayerData.PlayerProfileToVoiceNameMap = new();
        dto.PlayerProfileToVoiceNameMap.ForEach(entry =>
        {
            PlayerProfile playerProfile = GetPlayerProfile(entry.Key);
            if (playerProfile != null)
            {
                singScenePlayerData.PlayerProfileToVoiceNameMap[playerProfile] = entry.Value;
            }
        });
        return singScenePlayerData;
    }

    private PlayerProfile GetPlayerProfile(string profileName)
    {
        return settings.PlayerProfiles.FirstOrDefault(playerProfile => playerProfile.Name == profileName);
    }

    private MicProfile GetMicProfile(string profileName)
    {
        return settings.MicProfiles.FirstOrDefault(micProfile => micProfile.Name == profileName);
    }
    
    private string GetGameRoundDataErrorMessage(GameRoundData gameRoundData)
    {
        if (gameRoundData == null)
        {
            return "Missing game round data";
        }
        
        if (gameRoundData.SingScenePlayerData == null)
        {
            return "Missing player data";
        }

        if (gameRoundData.SingScenePlayerData.SelectedPlayerProfiles.IsNullOrEmpty())
        {
            return "Missing player profiles";
        }
        
        if (gameRoundData.SongMetas.IsNullOrEmpty())
        {
            return "Missing song metas";
        }
        
        return "";
    }
}
