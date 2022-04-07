using System;

public interface IConnectedClientHandler : IDisposable
{
    int SampleRateHz { get; }
    string ClientId { get; }
    string ClientName { get; }
    int GetNewMicSamples(float[] sampleBuffer);
}
