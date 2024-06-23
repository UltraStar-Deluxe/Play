using System.IO;
using System.Net;

public static class HttpListenerRequestExtensions
{
    public static string GetBodyAsString(this HttpListenerRequest request)
    {
        if (!request.HasEntityBody)
        {
            return null;
        }

        using Stream body = request.InputStream;
        using StreamReader reader = new(body, request.ContentEncoding);
        return reader.ReadToEnd();
    }
}
