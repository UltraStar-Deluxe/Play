using System;

public class SingletonNotFoundException : Exception
{
    public SingletonNotFoundException(string message) : base(message)
    {
    }

    public SingletonNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
