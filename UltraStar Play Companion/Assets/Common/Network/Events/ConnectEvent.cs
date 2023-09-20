using System.Collections.Generic;
using System.Net;

public class ConnectEvent
{
    public bool IsSuccess { get; private set; }
    public int ConnectRequestCount { get; private set; }
    public int HttpServerPort { get; private set; }
    public string ErrorMessage { get; private set; }
    public IPEndPoint ServerIpEndPoint { get; private set; }
    public List<HttpApiPermission> Permissions { get; private set; }
    public List<GameRoundModifierDto> AvailableGameRoundModifierDtos { get; private set; }

    public ConnectEvent(
        int httpServerPort,
        IPEndPoint serverIpEndPoint,
        List<HttpApiPermission> permissions,
        List<GameRoundModifierDto> availableGameRoundModifierDtos)
    {
        IsSuccess = true;
        HttpServerPort = httpServerPort;
        ServerIpEndPoint = serverIpEndPoint;
        Permissions = permissions;
        AvailableGameRoundModifierDtos = availableGameRoundModifierDtos;
    }

    public ConnectEvent(int connectRequestCount, string errorMessage=null)
    {
        IsSuccess = false;
        ConnectRequestCount = connectRequestCount;
        ErrorMessage = errorMessage;
    }
}
