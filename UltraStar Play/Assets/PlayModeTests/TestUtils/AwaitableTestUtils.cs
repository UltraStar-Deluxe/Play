using System;
using UnityEngine;

public class AwaitableTestUtils
{
    public const int DefaultTimeoutInMillis = 10000;

    public static async Awaitable WaitForConditionAsync(Action action, WaitForConditionConfig config = null)
    {
        await WaitForConditionAsync(() =>
        {
            action();
            return true;
        }, config);
    }

    public static async Awaitable WaitForConditionAsync(Func<bool> condition, WaitForConditionConfig config = null)
    {
        config ??= new WaitForConditionConfig();

        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        while (TimeUtils.GetUnixTimeMilliseconds() < startTime + config.timeoutInMillis)
        {
            try
            {
                // Condition must return true without throwing an exception
                if (condition())
                {
                    return;
                }
            }
            catch (Exception e)
            {
                // Ignore, try again after delay
                Debug.LogException(e);
            }

            await Awaitable.WaitForSecondsAsync(config.delayBetweenAttemptsInMillis / 1000f);
        }

        if (!condition())
        {
            throw new TimeoutException($"'{config.description}' not met within {config.timeoutInMillis} ms");
        }
    }
}

public class WaitForConditionConfig
{
    public string description = "condition";
    public float timeoutInMillis = AwaitableTestUtils.DefaultTimeoutInMillis;
    public float delayBetweenAttemptsInMillis = 500;
}
