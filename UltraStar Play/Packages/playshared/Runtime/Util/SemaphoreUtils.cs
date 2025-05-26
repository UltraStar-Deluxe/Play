using System;
using System.Threading;
using UnityEngine;

public static class SemaphoreUtils
{
    public static void SleepUntilSemaphoreIsFree(SemaphoreSlim semaphore, string taskName, TimeSpan maxWaitTime)
    {
        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        while (semaphore.CurrentCount > 0
               && TimeUtils.GetUnixTimeMilliseconds() - startTime < maxWaitTime.Milliseconds)
        {
            Debug.Log($"Waiting for background task to finish. taskName: '{taskName}', semaphore count: {semaphore.CurrentCount}");
            Thread.Sleep(500);
        }
    }
}
