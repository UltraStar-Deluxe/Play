using System.Threading;
using UniRx;
using System;

public static class ThreadUtils
{
    public static bool IsMainThread()
    {
        return Thread.CurrentThread.ManagedThreadId == 1;
    }

    public static void RunOnMainThread(Action action)
    {
        MainThreadDispatcher.Send(_ => action(), null);
    }

    public static void Sleep(int millis)
    {
        Thread.Sleep(millis);
    }
}
