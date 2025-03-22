public class VideoLoadedEvent
{
    public string VideoUri { get; private set; }

    public VideoLoadedEvent(string videoUri)
    {
        VideoUri = videoUri;
    }
}
