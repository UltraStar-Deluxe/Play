using System.Net;
using System.Net.Sockets;

public static class TcpListenerExtensions
{
    public static int GetPort(this TcpListener tcpListener)
    {
        if (tcpListener == null)
        {
            return -1;
        }
        return (tcpListener.LocalEndpoint as IPEndPoint).Port;
    }
}
