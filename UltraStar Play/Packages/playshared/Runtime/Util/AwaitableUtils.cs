using System;
using System.Buffers.Text;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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

    public static async Awaitable SendWebRequestAsync(UnityWebRequest unityWebRequest)
    {
        void LogSuccess()
        {
            Log.Verbose(() => $"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}");
        }

        void LogError(Exception ex)
        {
            Debug.LogError($"{unityWebRequest.method} '{unityWebRequest.uri}' has failed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, error message: {ex.Message}");
            Debug.LogException(ex);
        }

        try
        {
            await unityWebRequest.SendWebRequest();

            if (unityWebRequest.result is UnityWebRequest.Result.Success)
            {
                LogSuccess();
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
    }

    public static async Awaitable<string> GetWebRequestResponseAsync(UnityWebRequest unityWebRequest)
    {
        await SendWebRequestAsync(unityWebRequest);
        return unityWebRequest.downloadHandler?.text;
    }
}
