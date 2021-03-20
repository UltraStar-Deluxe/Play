using UnityEngine;
using SimpleHttpServerForUnity;

public class UltraStarPlayHttpServer : HttpServer
{
    protected override void Awake()
    {
        InitSingleInstance();
        host = IpAddressUtils.GetIpAddress(AddressFamily.IPv4);
        StartHttpListener();
    }
}
