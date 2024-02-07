using System;
using UnityEngine;

public class WaitUntilWithTimeout : CustomYieldInstruction
{
    private readonly Func<bool> predicate;
    private readonly long startTimeInMillis;
    private readonly TimeSpan timeout;
    private readonly string description;

    public override bool keepWaiting
    {
        get
        {
            if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, (long)timeout.TotalMilliseconds))
            {
                throw new TimeoutException($"Condition '{description}' was not met within {timeout} ms.");
            }
            return !predicate();
        }
    }

    public WaitUntilWithTimeout(string description, TimeSpan timeout, Func<bool> predicate)
    {
        this.description = description;
        this.timeout = timeout;
        this.predicate = predicate;
        startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
    }
}

