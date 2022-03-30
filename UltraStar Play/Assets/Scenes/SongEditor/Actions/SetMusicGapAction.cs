using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SetMusicGapAction : INeedInjection
{

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void Execute()
    {
        float positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
        songMeta.Gap = positionInSongInMillis;
    }

    public void ExecuteAndNotify()
    {
        Execute();
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Gap));
    }
}
