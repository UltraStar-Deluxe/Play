using UnityEngine;

public struct MicProfileDto : JsonSerializable
{
    public string Name { get; set; }
    public int ChannelIndex { get; set; }
    public Color32 Color { get; set; }
    public int Amplification { get; set; }
    public int NoiseSuppression { get; set; }
    public bool IsEnabled { get; set; }
    public int DelayInMillis { get; set; }
    public int SampleRate { get; set; }
    public string ConnectedClientId { get; set; }
}
