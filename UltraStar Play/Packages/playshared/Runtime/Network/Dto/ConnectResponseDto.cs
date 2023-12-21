using System.Collections.Generic;

public class ConnectResponseDto : CompanionAppMessageDto
{
    public string ClientName { get; set; }
    public string ClientId { get; set; }
    public string ErrorMessage { get; set; }
    public int HttpServerPort { get; set; }
    public List<HttpApiPermission> Permissions { get; set; }
    public List<GameRoundModifierDto> AvailableGameRoundModifierDtos { get; set; }

    public ConnectResponseDto() : base(CompanionAppMessageType.ConnectResponse)
    {
    }
}
