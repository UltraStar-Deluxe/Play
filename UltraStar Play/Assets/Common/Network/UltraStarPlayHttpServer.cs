using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

public class UltraStarPlayHttpServer : HttpServer, INeedInjection
{
    protected override void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        InitSingleInstance();
        if (Instance != this)
        {
            return;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying
            || Instance != this)
        {
            return;
        }

        Settings settings = SettingsManager.Instance.Settings;
        host = !settings.OwnHost.IsNullOrEmpty()
            ? settings.OwnHost
            : IpAddressUtils.GetIpAddress(AddressFamily.IPv4, NetworkInterfaceType.Wireless80211);

        NoEndpointFoundCallback = SendNoEndpointFound;
        StartHttpListener();

        this.CreateEndpoint(HttpMethod.Get, "api/rest/songs")
            .SetDescription("Get loaded songs")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(SendLoadedSongs);

        this.CreateEndpoint(HttpMethod.Get, "/api/rest/hello/{name}")
            .SetDescription("Say hello (path-parameter example)")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(SendHello);
    }

    private void SendHello(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new MessageDto
        {
            Message = "Hello " + requestData.PathParameters["name"]
        }.ToJson());
    }

    private void SendRegisteredEndpoints(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new EndpointListDto
        {
            Endpoints = GetRegisteredEndpoints()
                .Select(endpoint => new EndpointDto
                {
                    HttpMethod = endpoint.HttpMethod.Method,
                    UrlPattern = endpoint.PathPattern,
                    Description = endpoint.Description
                })
                .ToList()
        }.ToJson());
    }
    
    private void SendLoadedSongs(EndpointRequestData requestData)
    {
        SongMetaManager songMetaManager = SongMetaManager.Instance;
        requestData.Context.Response.SendResponse(new LoadedSongsDto
        {
            IsSongScanFinished = SongMetaManager.IsSongScanFinished,
            SongCount = songMetaManager.GetSongMetas().Count,
            SongList = songMetaManager.GetSongMetas()
                .Select(songMeta => new SongDto
                {
                    Artist = songMeta.Artist,
                    Title = songMeta.Title,
                    Hash = songMeta.SongHash,
                })
                .ToList()
        }.ToJson());
    }
    
    private static void SendNoEndpointFound(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new ErrorMessageDto
        {
            ErrorMessage = $"No endpoint found for '{requestData.Context.Request.HttpMethod}' on '{requestData.Context.Request.RawUrl}'. "
                + "Try 'GET' on 'api/rest/endpoints' to get the available endpoints."
        }.ToJson(), HttpStatusCode.NotFound);
    }

    public List<HttpApiPermission> GetPermissions(EndpointRequestData requestData)
    {
        string clientId = requestData.Context.Request.Headers["client-id"];
        if (clientId.IsNullOrEmpty())
        {
            return new();
        }
        
        Settings settings = SettingsManager.Instance.Settings;
        if (settings.HttpApiPermissions.TryGetValue(clientId, out List<HttpApiPermission> permissions))
        {
            return permissions;
        }

        return new();
    }
}
