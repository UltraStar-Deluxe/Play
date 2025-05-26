using System;
using UnityEngine;

public static class AwaitableUtils
{
    public static async Awaitable ExecuteAfterDelayInFramesAsync(int delayInFrames, Action action)
    {
        for (int i = 0; i < delayInFrames; i++)
        {
            await Awaitable.NextFrameAsync();
        }
        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteAfterDelayInFramesAsync(GameObject gameObject, int delayInFrames, Action action)
    {
        for (int i = 0; i < delayInFrames; i++)
        {
            await Awaitable.NextFrameAsync();
        }

        if (!gameObject)
        {
            return;
        }

        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteAfterDelayInSecondsAsync(float delayInSeconds, Action action)
    {
        await Awaitable.WaitForSecondsAsync(delayInSeconds);
        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteAfterDelayInSecondsAsync(GameObject gameObject, float delayInSeconds, Action action)
    {
        await Awaitable.WaitForSecondsAsync(delayInSeconds);

        if (!gameObject)
        {
            return;
        }

        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteRepeatedlyInSecondsAsync(GameObject gameObject, float delayInSeconds, Action action)
    {
        string gameObjectName = gameObject.name;

        // Loop while GameObject is alive (i.e., exit loop when GameObject has been destroyed)
        Log.Debug(() => $"Starting loop until GameObject '{gameObjectName}' has been destroyed");
        while (gameObject)
        {
            action();
            await Awaitable.WaitForSecondsAsync(delayInSeconds);
        }
        Log.Debug(() => $"Exited loop because until GameObject '{gameObjectName}' has been destroyed");
    }
}
