using System;
using UnityEngine;

public static class ExceptionExtensions
{
    public static Exception Log(this Exception ex)
    {
        Debug.LogException(ex);
        return ex;
    }

    public static Exception Log(this Exception ex, string message)
    {
        Debug.LogException(ex);
        Debug.LogError($"{message}: {ex.Message}");
        return ex;
    }
}
