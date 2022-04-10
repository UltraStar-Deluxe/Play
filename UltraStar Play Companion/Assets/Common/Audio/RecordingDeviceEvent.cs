public class RecordingDeviceEvent
{
    public string DeviceName { get; private set; }
    public int MinSampleRateHz { get; private set; }
    public int MaxSampleRateHz { get; private set; }
    public int CurrentSampleRate { get; private set; }

    public RecordingDeviceEvent(string deviceName, int minSampleRateHz, int maxSampleRateHz, int currentSampleRate)
    {
        this.DeviceName = deviceName;
        this.MinSampleRateHz = minSampleRateHz;
        this.MaxSampleRateHz = maxSampleRateHz;
        this.CurrentSampleRate = currentSampleRate;
    }
}
