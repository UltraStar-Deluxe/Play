using System;

public class AudioSupportProviderException : Exception
{
    public AudioSupportProviderException(string message) : base(message)
    {
    }

    public AudioSupportProviderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
