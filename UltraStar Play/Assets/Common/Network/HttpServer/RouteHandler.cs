using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

public class RouteHandler
{
    public static Comparison<RouteHandler> CompareDescendingByPlaceholderCount { get; private set; } =
        (a, b) => b.GetPlaceholderCount().CompareTo(a.GetPlaceholderCount());

    private Func<RouteRequestData, string> responseTextProvider;
    private CurlyBracePlaceholderMatcher patternMatcher;
    private HttpMethod httpMethod;

    public RouteHandler(HttpMethod httpMethod, string routePattern, Func<RouteRequestData, string> responseTextProvider)
    {
        this.patternMatcher = new CurlyBracePlaceholderMatcher(routePattern);
        this.responseTextProvider = responseTextProvider;
        this.httpMethod = httpMethod;
    }

    public int GetPlaceholderCount()
    {
        return patternMatcher.GetPlaceholderCount();
    }

    public bool TryHandle(HttpListenerRequest request, out string responseString)
    {
        if (string.Equals(request.HttpMethod, httpMethod.Method, StringComparison.OrdinalIgnoreCase)
            && patternMatcher.TryMatch(request.RawUrl, out Dictionary<string, string> placeholderValues))
        {
            RouteRequestData routeRequestData = new RouteRequestData(request, placeholderValues);
            responseString = responseTextProvider(routeRequestData);
            return true;
        }

        responseString = null;
        return false;
    }
}
