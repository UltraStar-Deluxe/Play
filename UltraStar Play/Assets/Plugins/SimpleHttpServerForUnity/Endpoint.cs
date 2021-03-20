using System.Net.Http;

namespace SimpleHttpServerForUnity
{
    public class Endpoint
    {
        public HttpMethod HttpMethod { get; private set; }
        public string UrlPattern { get; private set; }
        public string Description { get; private set; }

        public Endpoint(HttpMethod httpMethod, string urlPattern, string description)
        {
            HttpMethod = httpMethod;
            UrlPattern = urlPattern;
            Description = description;
        }
    }
}
