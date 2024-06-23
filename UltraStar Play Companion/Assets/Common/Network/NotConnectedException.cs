using System;

public class NotConnectedException : Exception
{
    public NotConnectedException(string message) : base(message)
    {
    }

    public NotConnectedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
