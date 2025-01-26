using System;
using UnityEngine;

public class ConditionUtils
{
    public const int DefaultTimeoutInMillis = 10000;

    public static async Awaitable<T> WaitForObjectAsync<T>(Func<T> getter, WaitForConditionConfig config = null)
    {
        await WaitForConditionAsync(() => getter() != null,
            new WaitForConditionConfig(config) { description = $"wait for object '{config.description}'" });
        return getter();
    }

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

            await Awaitable.WaitForSecondsAsync((float)(config.delayBetweenAttemptsInMillis / 1000.0));
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
    public double timeoutInMillis = ConditionUtils.DefaultTimeoutInMillis;
    public double delayBetweenAttemptsInMillis = 500;

    public WaitForConditionConfig()
    {
    }

    public WaitForConditionConfig(WaitForConditionConfig config)
    {
        description = config.description;
        timeoutInMillis = config.timeoutInMillis;
        delayBetweenAttemptsInMillis = config.delayBetweenAttemptsInMillis;
    }
}
