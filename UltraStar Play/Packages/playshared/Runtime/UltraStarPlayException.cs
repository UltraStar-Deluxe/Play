using System;

public class UltraStarPlayException : Exception
{
    public UltraStarPlayException(string message) : base(message)
    {
    }

    public UltraStarPlayException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
