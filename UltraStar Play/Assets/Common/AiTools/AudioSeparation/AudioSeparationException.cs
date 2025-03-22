using System;

public class AudioSeparationException : Exception
{
    public AudioSeparationException(string message) : base(message)
    {
    }

    public AudioSeparationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
