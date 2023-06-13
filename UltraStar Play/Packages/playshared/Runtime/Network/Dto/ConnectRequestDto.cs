public class ConnectRequestDto : CompanionAppMessageDto
{
    public int ProtocolVersion { get; set; }
    public string ClientName { get; set; }
    public string ClientId { get; set; }

    public ConnectRequestDto() : base(CompanionAppMessageType.ConnectRequest)
    {
    }
}
