using System;

public class ParentThemeNotFoundException : Exception
{
    public ParentThemeNotFoundException(string message) : base(message)
    {
    }

    public ParentThemeNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
