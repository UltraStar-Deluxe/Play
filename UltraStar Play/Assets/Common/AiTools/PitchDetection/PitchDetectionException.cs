using System;

public class PitchDetectionException : Exception
{
    public PitchDetectionException(string message) : base(message)
    {
    }

    public PitchDetectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
