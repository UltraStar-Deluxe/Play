using System;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public static class ThreadUtils
{
    public static bool IsMainThread()
    {
        return Thread.CurrentThread.ManagedThreadId == 1;
    }

    public static void RunOnMainThread(Action action)
    {
        if (IsMainThread())
        {
            action();
        }
        else
        {
            MainThreadDispatcher.Send(_ => action(), null);
        }
    }

    public static void RunOnBackgroundThread(Action action)
    {
        if (!IsMainThread())
        {
            action();
        }
        else
        {
            Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to run task on background thread: {ex.Message}");
                }
            });
        }
    }

    public static void Sleep(int millis)
    {
        Thread.Sleep(millis);
    }
}
