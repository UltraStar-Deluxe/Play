using System.Net;

public class ConnectResponseDto : JsonSerializable
{
    public string ClientName { get; set; }
    public string ClientId { get; set; }
    public string ErrorMessage { get; set; }
    public int MessagingPort { get; set; }
    public int HttpServerPort { get; set; }
    public IPEndPoint ServerIpEndPoint { get; set; }
}
