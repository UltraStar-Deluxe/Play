using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueRestControl : AbstractRestControl, INeedInjection
{
    public static SongQueueRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongQueueRestControl>();
    
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;
    
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
        httpServer.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.AvailablePlayers)
            .SetDescription($"Get player profiles that can be used for singing")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                List<string> playerProfileNames = settings.PlayerProfiles
                    .Where(playerProfile => playerProfile.IsEnabled)
                    .Select(playerProfile => playerProfile.Name)
                    .ToList();
                ListDto<string> dto = new()
                {
                    Items = playerProfileNames,
                };
                requestData.Context.Response.WriteJson(dto);
            });

        httpServer.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.AvailableMicrophones)
            .SetDescription($"Get microphone profiles that can be used for singing")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                List<MicProfile> enabledMicrophoneProfiles = settings.MicProfiles
                    .Where(microphoneProfile => microphoneProfile.IsEnabledAndConnected(serverSideConnectRequestManager))
                    .ToList();
                ListDto<MicProfile> dto = new()
                {
                    Items = enabledMicrophoneProfiles,
                };
                requestData.Context.Response.WriteJson(dto);
            });

        httpServer.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.SongQueue)
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
        
        httpServer.CreateEndpoint(HttpMethod.Post, HttpApiEndpointPaths.SongQueueEntry)
            .SetDescription($"Add entry song queue to the song queue.")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteSongQueue)
            .SetCallbackAndAdd(requestData =>
            {
                string json = requestData.Context.Request.GetBodyAsString();
                SongQueueEntryDto songQueueEntryDto = JsonConverter.FromJson<SongQueueEntryDto>(json, false);

                string errorMessage = songQueueManager.GetSongQueueEntryErrorMessage(songQueueEntryDto);
                if (!errorMessage.IsNullOrEmpty())
                {
                    Debug.LogError($"Invalid song queue entry: {errorMessage}");
                    return;
                }
                
                songQueueManager.AddSongQueueEntry(songQueueEntryDto);
            });
    }
}
