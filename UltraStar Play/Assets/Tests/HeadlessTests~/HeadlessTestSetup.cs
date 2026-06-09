using System;
using NUnit.Framework;
using UnityEngine;

[SetUpFixture]
public class HeadlessTestSetup
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        Debug.unityLogger.logHandler = new ConsoleLogHandler();
    }
}

public class ConsoleLogHandler : ILogHandler
{
    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        Console.WriteLine($"[{logType}] " + string.Format(format, args));
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        Console.WriteLine(exception.ToString());
    }
}
