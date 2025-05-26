using System;

public class UltraStarSongWriterException : Exception
{
    public UltraStarSongWriterException(string message)
        : base(message)
    {
    }

    public UltraStarSongWriterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
