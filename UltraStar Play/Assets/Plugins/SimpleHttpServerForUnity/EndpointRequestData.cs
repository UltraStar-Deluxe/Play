using System.Collections.Generic;
using System.Net;

namespace SimpleHttpServerForUnity
{
    public struct EndpointRequestData
    {
        public Dictionary<string, string> PathParameters { get; private set; }
        public HttpListenerContext Context { get; private set; }

        public EndpointRequestData(HttpListenerContext context, Dictionary<string, string> pathParameters)
        {
            PathParameters = pathParameters;
            Context = context;
        }
    }
}
