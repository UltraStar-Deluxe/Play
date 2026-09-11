using System;

public class ForcedAlignmentException : Exception
{
    public ForcedAlignmentException(string message) : base(message)
    {
    }

    public ForcedAlignmentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
