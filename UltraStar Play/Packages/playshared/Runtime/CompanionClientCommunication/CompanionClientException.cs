using System;

public class CompanionClientException : Exception
{
    public CompanionClientException(string message) : base(message)
    {
    }

    public CompanionClientException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
