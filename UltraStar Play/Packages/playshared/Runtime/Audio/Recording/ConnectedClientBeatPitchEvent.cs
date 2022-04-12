public class ConnectedClientBeatPitchEvent : BeatPitchEvent
{
    public string ClientId { get; set; }

    public ConnectedClientBeatPitchEvent(int midiNote, int beat, string clientId)
        : base(midiNote, beat)
    {
        ClientId = clientId;
    }
}
