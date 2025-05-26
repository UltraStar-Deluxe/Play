using System.Net;
using System.Net.Sockets;
using System.Text;

public static class UdpClientExtensions
{
    public static void Send(this UdpClient udpClient, string message, IPEndPoint ipEndPoint)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(message);
        udpClient.Send(responseBytes, responseBytes.Length, ipEndPoint);
    }

    public static int GetPort(this UdpClient udpClient)
    {
        if (udpClient == null)
        {
            return -1;
        }
        return (udpClient.Client.LocalEndPoint as IPEndPoint).Port;
    }
}
