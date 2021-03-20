using System;
using System.Net.Http;
using UnityEngine;

namespace SimpleHttpServerForUnity
{
    public static class HttpServerExtensions
    {
        public static void RegisterEndpoint(this HttpServer httpServer, GameObject gameObject, HttpMethod httpMethod, string urlPattern, string description, Action<EndpointRequestData> requestCallback)
        {
            httpServer.RegisterEndpoint(new EndpointHandler(httpMethod, urlPattern, description, requestCallback));
            
            RemoveEndpointOnDestroy removeEndpointOnDestroy = gameObject.AddComponent<RemoveEndpointOnDestroy>();
            removeEndpointOnDestroy.httpMethod = httpMethod;
            removeEndpointOnDestroy.urlPattern = urlPattern;
            removeEndpointOnDestroy.httpServer = httpServer;
        }
        
        public static void RegisterEndpoint(this HttpServer httpServer, HttpMethod httpMethod, string urlPattern, string description, Action<EndpointRequestData> requestCallback)
        {
            httpServer.RegisterEndpoint(new EndpointHandler(httpMethod, urlPattern, description, requestCallback));
        }

        public static void RemoveEndpoint(this HttpServer httpServer, EndpointHandler endpointHandler)
        {
            httpServer.RemoveEndpoint(endpointHandler.HttpMethod, endpointHandler.UrlPattern);
        }
    }

    public class RemoveEndpointOnDestroy : MonoBehaviour
    {
        public HttpServer httpServer;
        public HttpMethod httpMethod;
        public string urlPattern;
        
        private void OnDestroy()
        {
            if (httpServer != null)
            {
                httpServer.RemoveEndpoint(httpMethod, urlPattern);
            }
        }
    }
}
