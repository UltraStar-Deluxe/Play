using System;

[Serializable]
public class SongBuilderException : Exception 
{
    public SongBuilderException()
    {
    }

    public SongBuilderException(string message)
        : base(message) 
    {
    }

    public SongBuilderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
