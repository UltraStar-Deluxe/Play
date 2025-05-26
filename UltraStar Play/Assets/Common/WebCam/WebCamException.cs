using System;

public class WebCamException : Exception
{
    public WebCamException(string message) : base(message)
    {
    }

    public WebCamException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
