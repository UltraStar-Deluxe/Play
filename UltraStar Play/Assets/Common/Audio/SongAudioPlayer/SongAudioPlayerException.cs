using System;

public class SongAudioPlayerException : Exception
{
    public SongAudioPlayerException(string message) : base(message)
    {
    }

    public SongAudioPlayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
