using System;

public class JobDisposable :IDisposable
{
    private readonly Job job;

    private bool isDisposed;

    public JobDisposable(Job job)
    {
        this.job = job;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        job?.Cancel();
    }
}
