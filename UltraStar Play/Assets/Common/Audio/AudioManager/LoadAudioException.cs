using System;

public class LoadAudioException : Exception
{
    public LoadAudioException(string message) : base(message)
    {
    }

    public LoadAudioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
