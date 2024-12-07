using System;
using System.Threading;
using UnityEngine;

public static class AwaitableUtils
{
    public static async Awaitable WaitForCondition(string description, TimeSpan timeout, Func<bool> condition, CancellationToken? cancellationToken = null)
    {
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        while(!condition())
        {
            if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, (long)timeout.TotalMilliseconds))
            {
                throw new TimeoutException($"Condition '{description}' was not met within {timeout} ms.");
            }

            await Awaitable.NextFrameAsync();
        }
    }
}

