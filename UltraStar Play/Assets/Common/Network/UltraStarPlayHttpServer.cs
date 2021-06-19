using UnityEngine;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using SimpleHttpServerForUnity;

public class UltraStarPlayHttpServer : HttpServer
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
        
        host = IpAddressUtils.GetIpAddress(AddressFamily.IPv4, NetworkInterfaceType.Wireless80211);
        NoEndpointFoundCallback = SendNoEndpointFound;
        StartHttpListener();
        
        this.On(HttpMethod.Get, "api/rest/endpoints")
            .WithDescription("Get currently registered endpoints")
            .UntilDestroy(gameObject)
            .Do(SendRegisteredEndpoints);
        
        this.On(HttpMethod.Get, "api/rest/songs")
            .WithDescription("Get loaded songs")
            .UntilDestroy(gameObject)
            .Do(SendLoadedSongs);
        
        this.On(HttpMethod.Get, "/api/rest/hello/{name}")
            .WithDescription("Say hello (path-parameter example)")
            .UntilDestroy(gameObject)
            .Do(SendHello);
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
}
