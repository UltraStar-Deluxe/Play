using System;

public class LoadModException : Exception
{
    public LoadModException(string message) : base(message)
    {
    }

    public LoadModException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
