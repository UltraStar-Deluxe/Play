using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// Unity's implementation of ThreadPool allocates 1 KB per call to QueueUserWorkItem, so we have to
// roll our own.

public static class ThreadPool
{
    private static readonly Queue<(Action<PoolHandle>, PoolHandle)> queue;
    private static int waiterCount;
    private static readonly EventWaitHandle waitHandle;

    static ThreadPool()
    {
        queue = new Queue<(Action<PoolHandle>, PoolHandle)>();
        waiterCount = Mathf.Max(SystemInfo.processorCount - 1, 1);
        waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        for (int i = 0; i < waiterCount; i++)
        {
            var thread = new Thread(ThreadProc);
            thread.IsBackground = true;
            thread.Start();
        }
    }

    public static PoolHandle QueueUserWorkItem(Action<PoolHandle> callBack)
    {
        if (callBack == null)
            throw new ArgumentNullException("callBack");
        PoolHandle handle = new PoolHandle();
        lock (queue)
            queue.Enqueue((callBack, handle));
        if (waiterCount > 0)
            waitHandle.Set();
        return handle;
    }

    private static void ThreadProc()
    {
        while (true)
        {
            waitHandle.WaitOne();
            Interlocked.Decrement(ref waiterCount);
            while (true)
            {
                (Action<PoolHandle>, PoolHandle) callBack;
                lock (queue)
                {
                    if (queue.Count == 0)
                        break;
                    callBack = queue.Dequeue();
                }
                callBack.Item1(callBack.Item2);
            }
            Interlocked.Increment(ref waiterCount);
        }
    }

    public class PoolHandle
    {
        public bool done;
    }
}