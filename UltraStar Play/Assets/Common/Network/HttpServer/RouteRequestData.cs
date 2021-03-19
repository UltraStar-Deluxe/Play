using System.Collections.Generic;
using System.Net;

public struct RouteRequestData
{
    public Dictionary<string, string> PlaceholderValues { get; private set; }
    public HttpListenerRequest Request { get; private set; }

    public RouteRequestData(HttpListenerRequest request, Dictionary<string, string> placeholderValues)
    {
        PlaceholderValues = placeholderValues;
        Request = request;
    }
}
