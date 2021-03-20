using System.Net;
using System.Text;

namespace SimpleHttpServerForUnity
{
    public static class HttpListenerResponseExtensions
    {
        public static void SendResponse(this HttpListenerResponse response, string responseBody)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseBody);
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

        public static void SendResponse(this HttpListenerResponse response, string responseBody,
            HttpStatusCode statusCodeEnum)
        {
            response.SetStatusCode(statusCodeEnum);
            response.SendResponse(responseBody);
        }

        public static void SetStatusCode(this HttpListenerResponse response, HttpStatusCode statusCodeEnum)
        {
            response.StatusCode = (int)statusCodeEnum;
        }
    }
}
