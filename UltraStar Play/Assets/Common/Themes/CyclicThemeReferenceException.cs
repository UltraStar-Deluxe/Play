using System;

public class CyclicThemeReferenceException : Exception
{
    public CyclicThemeReferenceException(string message) : base(message)
    {
    }

    public CyclicThemeReferenceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
