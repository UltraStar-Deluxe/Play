using System;
using UnityEngine;

public class ExceptionUtils
{
    public static void LogThenThrow(Exception ex)
    {
        Debug.LogException(ex);
        throw ex;
    }

    public static void LogExceptionAndError(string message, Exception ex)
    {
        Debug.LogException(ex);
        Debug.LogError($"${message}: {ex.Message}");
    }
}
