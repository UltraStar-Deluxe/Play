using System;
using UnityEngine;
using UnityEngine.TestTools;

public static class LogAssertUtils
{
    public static void IgnoreFailingMessages()
    {
        try
        {
            LogAssert.ignoreFailingMessages = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to ignore failing messages: " + e.Message);
        }
    }
}
