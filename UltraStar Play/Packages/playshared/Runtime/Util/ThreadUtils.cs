using System;
using System.Threading;
using UniRx;

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

    public static void Sleep(int millis)
    {
        Thread.Sleep(millis);
    }
}
