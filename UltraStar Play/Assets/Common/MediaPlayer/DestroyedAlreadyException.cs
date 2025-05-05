using System;

public class DestroyedAlreadyException : Exception
{
    public DestroyedAlreadyException(string message) : base(message)
    {
    }

    public DestroyedAlreadyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
