using System;

public class UltraStarSongParserException : Exception
{
    public UltraStarSongParserException(string message)
        : base(message)
    {
    }

    public UltraStarSongParserException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
