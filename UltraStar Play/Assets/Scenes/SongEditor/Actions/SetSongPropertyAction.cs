using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SetSongPropertyAction : INeedInjection
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

    public void SetMedleyStart(double positionInMillis)
    {
        int beat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, positionInMillis);
        songMeta.MedleyStartInMillis = beat;
    }

    public void SetMedleyEnd(double positionInMillis)
    {
        int beat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, positionInMillis);
        songMeta.MedleyEndInMillis = beat;
    }

    public void SetMedleyStartAndNotify(double positionInMillis)
    {
        SetMedleyStart(positionInMillis);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.MedleyStart));
    }

    public void SetMedleyEndAndNotify(double positionInMillis)
    {
        SetMedleyEnd(positionInMillis);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.MedleyEnd));
    }
}
