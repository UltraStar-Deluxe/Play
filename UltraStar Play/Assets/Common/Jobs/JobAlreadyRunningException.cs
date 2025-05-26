using System;

public class JobAlreadyRunningException : Exception
{
    public JobAlreadyRunningException(string message) : base(message)
    {
    }

    public JobAlreadyRunningException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public JobAlreadyRunningException(Exception innerException) : this("Job already running", innerException)
    {
    }
}
