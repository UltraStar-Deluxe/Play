public class AudioLoadedEvent
{
    public string AudioUri { get; private set; }

    public AudioLoadedEvent(string audioUri)
    {
        AudioUri = audioUri;
    }
}
