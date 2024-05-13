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

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private PanelHelper panelHelper;

    public void Execute(double positionInMillis)
    {
        songMeta.GapInMillis = (float)positionInMillis;
    }

    public void ExecuteAndNotify(double positionInMillis)
    {
        Execute(positionInMillis);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Gap));
    }
}
