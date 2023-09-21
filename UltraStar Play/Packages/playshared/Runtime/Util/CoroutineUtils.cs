using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class CoroutineUtils
{
    public static IEnumerator WebRequestCoroutine(
        UnityWebRequest webRequest,
        Action<DownloadHandler> onSuccess,
        Action<Exception> onError,
        bool busyWaiting = false)
    {
        bool isDone = false;
        do
        {
            if (webRequest.result
                is UnityWebRequest.Result.ConnectionError
                or UnityWebRequest.Result.ProtocolError
                or UnityWebRequest.Result.DataProcessingError)
            {
                isDone = true;
                onError?.Invoke(new UnityWebRequestException(webRequest));
            }
            else if (webRequest.result is UnityWebRequest.Result.Success)
            {
                isDone = true;
                onSuccess?.Invoke(webRequest.downloadHandler);
            }

            if (!isDone)
            {
                if (busyWaiting)
                {
                    Debug.LogWarning($"Waiting for web request '{webRequest.uri}' to finish via Thread.Sleep");
                    Thread.Sleep(10);
                }
                else
                {
                    // Wait for next frame
                    yield return null;
                }
            }
        } while (!isDone);

        webRequest.Dispose();
    }

    public static IEnumerator ExecuteAction(Action action)
    {
        action();
        yield return null;
    }

    public static IEnumerator Sequence(params IEnumerator[] coroutines)
    {
        foreach (IEnumerator coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    public static IEnumerator ExecuteWhenConditionIsTrue(Func<bool> condition, Action action)
    {
        while (!condition())
        {
            yield return null;
        }
        action();
    }

    public static IEnumerator ExecuteAfterDelayInFrames(int delayInFrames, Action action)
    {
        for (int i = 0; i < delayInFrames; i++)
        {
            yield return null;
        }
        // Code to execute after the delay
        action();
    }

    public static IEnumerator ExecuteAfterDelayInSeconds(float delayInSeconds, Action action)
    {
        yield return new WaitForSeconds(delayInSeconds);
        // Code to execute after the delay
        action();
    }

    public static IEnumerator ExecuteRepeatedlyInSeconds(float delayInSeconds, Action action)
    {
        while (true)
        {
            action();
            yield return new WaitForSeconds(delayInSeconds);
        }
    }
}
