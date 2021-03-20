using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SimpleHttpServerForUnity
{
// https://stackoverflow.com/questions/51975799/how-to-get-ip-address-of-device-in-unity-2018
    public class IpAddressUtils
    {
        public static string GetIpAddress(AddressFamily addressFamily)
        {
            // Return null if AddressFamily is Ipv6 but Os does not support it
            if (addressFamily == AddressFamily.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = "localhost";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                NetworkInterfaceType type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == type1 || item.NetworkInterfaceType == type2) &&
                    item.OperationalStatus == OperationalStatus.Up)
#endif
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        //IPv4
                        if (addressFamily == AddressFamily.IPv4
                            && ip.Address.ToString() != "127.0.0.1")
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                output = ip.Address.ToString();
                            }
                        }

                        //IPv6
                        else if (addressFamily == AddressFamily.IPv6)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                                && ip.Address.ToString() != "127.0.0.1")
                            {
                                output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            return output;
        }
    }

    public enum AddressFamily
    {
        IPv4, IPv6
    }
}
