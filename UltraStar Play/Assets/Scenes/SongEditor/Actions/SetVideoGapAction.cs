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
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void Execute(float newVideoGap)
    {
        songMeta.VideoGap = newVideoGap;
        songVideoPlayer.SyncVideoWithMusic(songAudioPlayer.PositionInSongInMillis, true);
    }

    public void ExecuteAndNotify(float newVideoGap)
    {
        Execute(newVideoGap);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.VideoGap));
    }
}
