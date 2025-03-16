using System;

public class VideoSupportProviderException : Exception
{
    public VideoSupportProviderException(string message) : base(message)
    {
    }

    public VideoSupportProviderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
