using UnityEngine;

public interface ISettings
{
    public SystemLanguage Language { get; set; }
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; }

    public bool ShowFps { get; }

    /**
     * The UDP port on the server (e.g. Companion App) for initiating a connection.
     * Default value 34567.
     */
    public int UdpPortOnServer { get; set; }

    /**
     * The port on the client (e.g. Companion App) for initiating a connection.
     * Default value 34568.
     */
    public int UdpPortOnClient { get; set; }

    /**
     * The IP address of the device running this app.
     * May be empty to select an IP address automatically.
     */
    public string OwnHost { get; set; }
}
