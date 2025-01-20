using UnityEngine;
using UnityEngine.TestTools;
using Exception = System.Exception;

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
