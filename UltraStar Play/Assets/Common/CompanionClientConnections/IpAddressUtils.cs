using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

public class IpAddressUtils
{
    public static string GetIpAddress(SimpleHttpServerForUnity.AddressFamily addressFamily, params NetworkInterfaceType[] networkInterfaceTypes)
    {
        return SimpleHttpServerForUnity.IpAddressUtils.GetIpAddress(addressFamily, networkInterfaceTypes);
    }

    public static IPAddress GetLocalIpAddress()
    {
        try
        {
            // https://stackoverflow.com/questions/6803073/get-local-ip-address
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to determine local IP address by creating a connection with a socket. Maybe not connected to any LAN.");

            string localIpAddressAsString = IpAddressUtils.GetIpAddress(SimpleHttpServerForUnity.AddressFamily.IPv4);
            if (localIpAddressAsString.IsNullOrEmpty()
                || localIpAddressAsString == "localhost")
            {
                return IPAddress.Loopback;
            }

            return IPAddress.Parse(localIpAddressAsString);
        }
    }
}
