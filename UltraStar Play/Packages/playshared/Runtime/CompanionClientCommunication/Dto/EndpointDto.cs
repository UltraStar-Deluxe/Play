public class EndpointDto : JsonSerializable
{
    public string HttpMethod { get; set; }
    public string UrlPattern { get; set; }
    public string Description { get; set; }
}
