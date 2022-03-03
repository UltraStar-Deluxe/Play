using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorStatusBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.statusBarSongInfoLabel)]
    private Label statusBarSongInfoLabel;

    [Inject(UxmlName = R.UxmlNames.statusBarPositionInfoLabel)]
    private Label statusBarPositionInfoLabel;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public void OnInjectionFinished()
    {
        statusBarSongInfoLabel.text = $"{songMeta.Artist} - {songMeta.Title}";
        statusBarPositionInfoLabel.text = "";

        songAudioPlayer.PositionInSongEventStream
            .Subscribe(millis =>
            {
                TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, (int)millis);
                statusBarPositionInfoLabel.text = $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:00}";
            });
    }
}
