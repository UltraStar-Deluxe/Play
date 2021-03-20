using System;
using System.Net.Http;

namespace SimpleHttpServerForUnity
{
    public static class HttpServerExtensions
    {
        public static void AddEndpoint(this HttpServer httpServer, HttpMethod httpMethod, string urlPattern,
            Action<EndpointRequestData> requestCallback)
        {
            httpServer.RegisterEndpoint(new EndpointHandler(httpMethod, urlPattern, requestCallback));
        }

        public static void RemoveEndpoint(this HttpServer httpServer, EndpointHandler endpointHandler)
        {
            httpServer.RemoveEndpoint(endpointHandler.HttpMethod, endpointHandler.Pattern);
        }
    }
}
