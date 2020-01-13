using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

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
        double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
        songMeta.Gap = (float)positionInSongInMillis;
    }

    public void ExecuteAndNotify()
    {
        Execute();
        songMetaChangeEventStream.OnNext(new MusicGapChangedEvent());
    }
}
