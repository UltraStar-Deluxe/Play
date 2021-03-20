using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using SimpleHttpServerForUnity;

public class UltraStarPlayHttpServer : HttpServer
{
    protected override void Awake()
    {
        InitSingleInstance();
        if (Instance != this)
        {
            return;
        }
        
        host = IpAddressUtils.GetIpAddress(AddressFamily.IPv4);
        NoEndpointFoundCallback = SendNoEndpointFound;
        StartHttpListener();
        
        this.RegisterEndpoint(gameObject, HttpMethod.Get, "api/rest/endpoints",
            "Get currently registered endpoints",
            SendRegisteredEndpoints);
        
        this.RegisterEndpoint(gameObject, HttpMethod.Get, "api/rest/songs",
            "Get loaded songs",
            SendLoadedSongs);
        
        this.RegisterEndpoint(gameObject, HttpMethod.Get, "/api/rest/hello/{name}",
            "Say hello (path-parameter example)",
            SendHello);
    }

    private void SendHello(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new MessageDto
        {
            message = "Hello " + requestData.PathParameters["name"]
        }.ToJson());
    }

    private void SendRegisteredEndpoints(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new EndpointListDto()
        {
            endpoints = GetRegisteredEndpoints()
                .Select(endpoint => new EndpointDto
                {
                    httpMethod = endpoint.HttpMethod.Method,
                    urlPattern = endpoint.UrlPattern,
                    description = endpoint.Description
                })
                .ToList()
        }.ToJson());
    }
    
    private void SendLoadedSongs(EndpointRequestData requestData)
    {
        SongMetaManager songMetaManager = SongMetaManager.Instance;
        requestData.Context.Response.SendResponse(new LoadedSongsDto
        {
            isSongScanFinished = SongMetaManager.IsSongScanFinished,
            songCount = songMetaManager.GetSongMetas().Count,
            songList = songMetaManager.GetSongMetas()
                .Select(songMeta => new SongDto
                {
                    artist = songMeta.Artist,
                    title = songMeta.Title,
                    hash = songMeta.SongHash,
                })
                .ToList()
        }.ToJson());
    }
    
    private static void SendNoEndpointFound(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new ErrorMessageDto
        {
            errorMessage = $"No endpoint found for '{requestData.Context.Request.HttpMethod}' on '{requestData.Context.Request.RawUrl}'. "
                + "Try 'GET' on 'api/rest/endpoints' to get the available endpoints."
        }.ToJson(), HttpStatusCode.NotFound);
    }
}
