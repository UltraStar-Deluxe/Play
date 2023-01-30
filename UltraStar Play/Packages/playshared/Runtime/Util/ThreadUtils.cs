using System.Threading;

public static class ThreadUtils
{
    public static bool IsMainThread()
    {
        return Thread.CurrentThread.ManagedThreadId == 1;
    }
}
