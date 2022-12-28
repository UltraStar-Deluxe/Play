using System;

public class IllegalArgumentException : Exception
{
    public IllegalArgumentException(string message) : base(message)
    {
    }

    public IllegalArgumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
