using UniRx;
using UnityEngine;

public interface ISettings
{
    public ReactiveProperty<SystemLanguage> Language { get; }
    public ReactiveProperty<EPitchDetectionAlgorithm> PitchDetectionAlgorithm { get; }

    public ReactiveProperty<bool> ShowFps { get; }

    /**
     * The UDP port on the server (e.g. Companion App) for initiating a connection.
     * Default value 34567.
     */
    public ReactiveProperty<int> UdpPortOnServer { get; }

    /**
     * The port on the client (e.g. Companion App) for initiating a connection.
     * Default value 34568.
     */
    public ReactiveProperty<int> UdpPortOnClient { get; }

    /**
     * The IP address of the device running this app.
     * May be empty to select an IP address automatically.
     */
    public ReactiveProperty<string> OwnHost { get; }
}
