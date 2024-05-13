using System.Net;
using System.Text;

public static class HttpListenerResponseExtensions
{
    public static void WriteJson(this HttpListenerResponse response, object obj)
    {
        if (obj == null)
        {
            return;
        }

        string json = JsonConverter.ToJson(obj);
        response.WriteString(json);
    }

    public static void WriteString(this HttpListenerResponse response, string text)
    {
        if (text.IsNullOrEmpty())
        {
            return;
        }

        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        response.OutputStream.Write(textBytes);
    }
}
