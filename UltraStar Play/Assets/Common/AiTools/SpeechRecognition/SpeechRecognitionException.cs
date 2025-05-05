using System;

public class SpeechRecognitionException : Exception
{
    public SpeechRecognitionException(string message) : base(message)
    {
    }

    public SpeechRecognitionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
