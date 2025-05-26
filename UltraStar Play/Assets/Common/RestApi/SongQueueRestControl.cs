using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueRestControl : AbstractRestControl, INeedInjection
{
    public static SongQueueRestControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongQueueRestControl>();

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private SongQueueManager songQueueManager;

    [Inject]
    private SongMetaManager songMetaManager;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.AvailablePlayers)
            .SetDescription($"Get player profiles that can be used for singing")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                List<string> playerProfileNames = SettingsUtils.GetPlayerProfiles(settings, nonPersistentSettings)
                    .Where(playerProfile => playerProfile.IsEnabled)
                    .Select(playerProfile => playerProfile.Name)
                    .ToList();
                ListDto<string> dto = new()
                {
                    Items = playerProfileNames,
                };
                requestData.Context.Response.WriteJson(dto);
            });

        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.AvailableMicrophones)
            .SetDescription($"Get microphone profiles that can be used for singing")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                List<MicProfile> enabledMicrophoneProfiles = settings.MicProfiles
                    .Where(microphoneProfile => microphoneProfile.IsEnabledAndConnected(serverSideCompanionClientManager))
                    .ToList();
                ListDto<MicProfile> dto = new()
                {
                    Items = enabledMicrophoneProfiles,
                };
                requestData.Context.Response.WriteJson(dto);
            });

        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.SongQueue)
            .SetDescription($"Get song queue")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                List<SongQueueEntryDto> songQueueEntryDtos = songQueueManager.GetSongQueueEntries().ToList();
                ListDto<SongQueueEntryDto> dto = new()
                {
                    Items = songQueueEntryDtos,
                };
                requestData.Context.Response.WriteJson(dto);
            });

        httpServer.CreateEndpoint(HttpMethod.Post, RestApiEndpointPaths.SongQueue)
            .SetDescription($"Set song queue")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string json = requestData.Context.Request.GetBodyAsString();
                ListDto<SongQueueEntryDto> listDto = JsonConverter.FromJson<ListDto<SongQueueEntryDto>>(json);
                List<SongQueueEntryDto> songQueueEntryDtos = listDto.Items;

                for (int i = 0; i < songQueueEntryDtos.Count; i++)
                {
                    SongQueueEntryDto songQueueEntryDto = songQueueEntryDtos[i];
                    string errorMessage = songQueueManager.GetSongQueueEntryErrorMessage(songQueueEntryDto);
                    if (!errorMessage.IsNullOrEmpty())
                    {
                        Debug.LogError($"Invalid song queue entry at index {i}: {errorMessage}");
                        return;
                    }
                }

                songQueueManager.SetSongQueueEntries(songQueueEntryDtos);
            });

        httpServer.CreateEndpoint(HttpMethod.Post, RestApiEndpointPaths.SongQueueEntry)
            .SetDescription($"Add entry song queue to the song queue.")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteSongQueue, settings)
            .SetCallbackAndAdd(requestData =>
            {
                string json = requestData.Context.Request.GetBodyAsString();
                SongQueueEntryDto songQueueEntryDto = JsonConverter.FromJson<SongQueueEntryDto>(json);

                string errorMessage = songQueueManager.GetSongQueueEntryErrorMessage(songQueueEntryDto);
                if (!errorMessage.IsNullOrEmpty())
                {
                    Debug.LogError($"Invalid song queue entry: {errorMessage}");
                    return;
                }

                songQueueManager.AddSongQueueEntry(songQueueEntryDto);
            });

        httpServer.CreateEndpoint(HttpMethod.Delete, RestApiEndpointPaths.SongQueueEntryIndex)
            .SetDescription($"Remove song queue entry at given index.")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteSongQueue, settings)
            .SetCallbackAndAdd(requestData =>
            {
                if (!int.TryParse(requestData.PathParameters["index"], out int index)
                    || index < 0 || index >= songQueueManager.GetSongQueueEntries().Count)
                {
                    Debug.LogError("Cannot delete song queue entry. Invalid index");
                    return;
                }

                SongQueueEntryDto songQueueEntryDto = songQueueManager.GetSongQueueEntries()[index];
                songQueueManager.RemoveSongQueueEntry(songQueueEntryDto);
            });

        httpServer.CreateEndpoint(HttpMethod.Post, RestApiEndpointPaths.SongQueueEntryIndex)
            .SetDescription($"Update song queue entry at given index.")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteSongQueue, settings)
            .SetCallbackAndAdd(requestData =>
            {
                if (!int.TryParse(requestData.PathParameters["index"], out int index)
                    || index < 0 || index >= songQueueManager.GetSongQueueEntries().Count)
                {
                    Debug.LogError("Cannot update song queue entry. Invalid index");
                    return;
                }

                string json = requestData.Context.Request.GetBodyAsString();
                SongQueueEntryDto newSongQueueEntryDto = JsonConverter.FromJson<SongQueueEntryDto>(json);

                SongQueueEntryDto oldSongQueueEntryDto = songQueueManager.GetSongQueueEntries()[index];
                songQueueManager.UpdateSongQueueEntry(oldSongQueueEntryDto, newSongQueueEntryDto);
            });
    }
}
