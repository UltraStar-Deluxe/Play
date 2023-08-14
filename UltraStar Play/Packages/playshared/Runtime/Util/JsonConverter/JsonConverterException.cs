using System;

public class JsonConverterException : Exception
{
    public JsonConverterException(string message) : base(message)
    {
    }

    public JsonConverterException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
