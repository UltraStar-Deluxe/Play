using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace SimpleHttpServerForUnity
{
    public class EndpointHandler
    {
        public static Comparison<EndpointHandler> CompareDescendingByPlaceholderCount { get; private set; } =
            (a, b) => b.GetPlaceholderCount().CompareTo(a.GetPlaceholderCount());

        public HttpMethod HttpMethod { get; private set; }
        public string UrlPattern => patternMatcher.Pattern;
        public string Description { get; private set; }

        private readonly Action<EndpointRequestData> requestCallback;
        private readonly CurlyBracePlaceholderMatcher patternMatcher;

        public EndpointHandler(HttpMethod httpMethod, string urlPattern, string description, Action<EndpointRequestData> requestCallback)
        {
            this.patternMatcher = new CurlyBracePlaceholderMatcher(urlPattern);
            this.requestCallback = requestCallback;
            this.HttpMethod = httpMethod;
            this.Description = description;
        }

        public int GetPlaceholderCount()
        {
            return patternMatcher.GetPlaceholderCount();
        }

        public bool TryHandle(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            if (string.Equals(request.HttpMethod, HttpMethod.Method, StringComparison.OrdinalIgnoreCase)
                && patternMatcher.TryMatch(request.RawUrl, out Dictionary<string, string> placeholderValues))
            {
                EndpointRequestData endpointRequestData = new EndpointRequestData(context, placeholderValues);
                requestCallback(endpointRequestData);
                return true;
            }

            return false;
        }
    }
}
