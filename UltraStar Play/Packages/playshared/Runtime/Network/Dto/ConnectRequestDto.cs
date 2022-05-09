public class ConnectRequestDto : JsonSerializable
{
    public int ProtocolVersion { get; set; }
    public string ClientName { get; set; }
    public string ClientId { get; set; }
}
