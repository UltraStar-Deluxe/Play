using System;

public class LoadImageException : Exception
{
    public LoadImageException(string message) : base(message)
    {
    }

    public LoadImageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
