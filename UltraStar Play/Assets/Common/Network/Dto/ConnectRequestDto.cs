public class ConnectRequestDto : JsonSerializable
{
    public int ProtocolVersion { get; set; }
    public string ClientName { get; set; }
    public int MicrophoneSampleRate { get; set; }
}
