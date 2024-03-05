using System;

public class ActionProgress : IProgress<float>
{
    private readonly Action<float> onProgress;

    public ActionProgress(Action<float> onProgress)
    {
        this.onProgress = onProgress;
    }

    public void Report(float progress)
    {
        onProgress?.Invoke(progress);
    }
}
