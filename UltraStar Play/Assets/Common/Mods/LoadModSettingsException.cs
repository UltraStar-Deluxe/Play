using System;

public class LoadModSettingsException : Exception
{
    public string ModSettingsPath { get; set; }
    public string ModFolder { get; set; }

    public LoadModSettingsException(string message) : base(message)
    {
    }

    public LoadModSettingsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
