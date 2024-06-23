using System.Linq;
using System.Net;
using System.Net.Http;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

public class UltraStarPlayHttpServer : HttpServer, INeedInjection
{
    private const int DefaultPort = 6789;
    
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
        host = !settings.HttpServerHost.IsNullOrEmpty()
            ? settings.HttpServerHost
            : (IpAddressUtils.GetLocalIpAddress()?.ToString() ?? "localhost");
        
        port = settings.HttpServerPort > 0
            ? settings.HttpServerPort
            : DefaultPort;

        NoEndpointFoundCallback = SendNoEndpointFound;
        StartHttpListener();

        this.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.Endpoints)
            .SetDescription("Get currently registered endpoints")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(SendRegisteredEndpoints);

        this.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.Hello)
            .SetDescription("Say hello (path-parameter example)")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(SendHello);
    }

    public string GetExampleEndpoint()
    {
        return $"{host}:{port}/{HttpApiEndpointPaths.Songs}";
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
    
    private static void SendNoEndpointFound(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse(new ErrorMessageDto
        {
            ErrorMessage = $"No endpoint found for '{requestData.Context.Request.HttpMethod}' on '{requestData.Context.Request.RawUrl}'. "
                + "Try 'GET' on 'api/rest/endpoints' to get the available endpoints."
        }.ToJson(), HttpStatusCode.NotFound);
    }
}
