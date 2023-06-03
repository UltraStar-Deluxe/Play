using UniInject;
using UnityEngine;

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
    
    public void SetMedleyStart(double positionInSongInMillis)
    {
        int beat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);
        songMeta.MedleyStartBeat = beat;
    }
    
    public void SetMedleyEnd(double positionInSongInMillis)
    {
        int beat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);
        songMeta.MedleyEndBeat = beat;
    }

    public void SetMedleyStartAndNotify(double positionInSongInMillis)
    {
        SetMedleyStart(positionInSongInMillis);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.MedleyStart));
    }
    
    public void SetMedleyEndAndNotify(double positionInSongInMillis)
    {
        SetMedleyEnd(positionInSongInMillis);
        songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.MedleyEnd));
    }
}
