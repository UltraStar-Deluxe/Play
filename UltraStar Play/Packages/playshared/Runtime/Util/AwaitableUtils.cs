using System;
using UnityEngine;
using UnityEngine.Networking;

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

    public static async Awaitable ExecuteAfterDelayInFrames(GameObject gameObject, int delayInFrames, Action action)
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

    public static async Awaitable ExecuteAfterDelayInSeconds(float delayInSeconds, Action action)
    {
        await Awaitable.WaitForSecondsAsync(delayInSeconds);
        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteAfterDelayInSeconds(GameObject gameObject, float delayInSeconds, Action action)
    {
        await Awaitable.WaitForSecondsAsync(delayInSeconds);

        if (!gameObject)
        {
            return;
        }

        // Code to execute after the delay
        action();
    }

    public static async Awaitable ExecuteRepeatedlyInSeconds(GameObject gameObject, float delayInSeconds, Action action)
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

    public static async Awaitable ExecuteWhenConditionIsTrue(GameObject gameObject, Func<bool> condition, Action action)
    {
        while (!condition())
        {
            await Awaitable.NextFrameAsync();
            if (!gameObject)
            {
                // GameObject has been deleted, so exit this method.
                return;
            }
        }
        action();
    }

    public static async Awaitable<string> SendWebRequest(UnityWebRequest unityWebRequest)
    {
        void LogSuccess()
        {
            string responseBody = unityWebRequest.downloadHandler?.text;
            Log.Verbose(() => $"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, response body: {responseBody}");
        }

        void LogError(Exception ex)
        {
            string responseBody = unityWebRequest.downloadHandler?.text;
            Debug.LogError($"{unityWebRequest.method} '{unityWebRequest.uri}' has failed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, error message: {ex.Message}, response body: {responseBody}");
            Debug.LogException(ex);
        }

        try
        {
            await unityWebRequest.SendWebRequest();

            if (unityWebRequest.result is UnityWebRequest.Result.Success)
            {
                LogSuccess();
                return unityWebRequest.downloadHandler?.text;
            }
            else
            {
                string errorMessage = unityWebRequest.error ?? "Unknown error";
                Exception ex = new($"{unityWebRequest.result}: {errorMessage}");
                LogError(ex);
                throw new UnityWebRequestException(unityWebRequest);
            }
        }
        catch (Exception ex)
        {
            LogError(ex);
            throw ex;
        }
        finally
        {
            unityWebRequest.Dispose();
        }
    }
}
