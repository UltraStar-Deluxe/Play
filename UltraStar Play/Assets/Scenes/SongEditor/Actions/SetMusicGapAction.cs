using UniInject;
using UnityEngine;

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
    
    public void Execute(double positionInSongInMillis)
    {
        songMeta.Gap = (float)positionInSongInMillis;
    }

    public void ExecuteAndNotify(double positionInSongInMillis)
    {
        Execute(positionInSongInMillis);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Gap));
    }
}
