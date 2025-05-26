using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SetVideoGapAction : INeedInjection
{

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongVideoPlayer songVideoPlayer;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    public void Execute(double newVideoGap)
    {
        songMeta.VideoGapInMillis = newVideoGap;
        songVideoPlayer.SyncVideoPositionWithAudio(true);
    }

    public void ExecuteAndNotify(double newVideoGap)
    {
        Execute(newVideoGap);
        songMetaChangedEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.VideoGap));
    }
}
