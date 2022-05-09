public class CompanionAppMessageDto : JsonSerializable
{
    public CompanionAppMessageType MessageType { get; set; }

    public CompanionAppMessageDto()
    {
    }

    public CompanionAppMessageDto(CompanionAppMessageType messageType)
    {
        MessageType = messageType;
    }
}
