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
        host = !settings.OwnHost.Value.IsNullOrEmpty()
            ? settings.OwnHost.Value
            : IpAddressUtils.GetIpAddress(AddressFamily.IPv4, NetworkInterfaceType.Wireless80211);

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
