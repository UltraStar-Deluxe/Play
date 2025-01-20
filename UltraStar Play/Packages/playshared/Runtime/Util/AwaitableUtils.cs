using System;
using UnityEngine;

public static class AwaitableUtils
{
    public static async Awaitable ExecuteAfterDelayInFrames(int delayInFrames, Action action)
    {
        for (int i = 0; i < delayInFrames; i++)
        {
            await Awaitable.NextFrameAsync();
        }
        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteAfterDelayInSeconds(float delayInSeconds, Action action)
    {
        await Awaitable.WaitForSecondsAsync(delayInSeconds);
        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteRepeatedlyInSeconds(float delayInSeconds, Action action, GameObject gameObject)
    {
        string gameObjectName = gameObject.name;

        // Loop while until GameObject is alive (i.e., exit loop when GameObject has been destroyed)
        Log.Debug(() => $"Starting loop until GameObject '{gameObjectName}' has been destroyed");
        while (gameObject != null)
        {
            action();
            await Awaitable.WaitForSecondsAsync(delayInSeconds);
        }
        Log.Debug(() => $"Exited loop because until GameObject '{gameObjectName}' has been destroyed");
    }
}
