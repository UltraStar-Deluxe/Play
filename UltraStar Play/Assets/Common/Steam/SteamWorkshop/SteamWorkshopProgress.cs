using System;

public class SteamWorkshopProgress : IProgress<float>
{
    private readonly Action<float> onProgress;

    public SteamWorkshopProgress(Action<float> onProgress)
    {
        this.onProgress = onProgress;
    }

    public void Report(float progress)
    {
        onProgress?.Invoke(progress);
    }
}
