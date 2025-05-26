using System;

public class MidiToSongMetaException : Exception
{
    public MidiToSongMetaException(string message) : base(message)
    {
    }

    public MidiToSongMetaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
