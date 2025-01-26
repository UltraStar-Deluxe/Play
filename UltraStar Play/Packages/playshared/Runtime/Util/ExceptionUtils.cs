using System;
using UnityEngine;

public class ExceptionUtils
{
    public static void LogThenThrow(Exception ex)
    {
        Debug.LogException(ex);
        throw ex;
    }
}
