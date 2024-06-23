using System;

public class SongVideoPlayerException : Exception
{
    public SongVideoPlayerException(string message) : base(message)
    {
    }

    public SongVideoPlayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
