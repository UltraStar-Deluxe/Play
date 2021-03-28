using System.Collections.Generic;

public class EndpointListDto : JsonSerializable
{
    public List<EndpointDto> Endpoints { get; set; }
}
