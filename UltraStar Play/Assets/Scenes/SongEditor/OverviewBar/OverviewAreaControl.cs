using System;
using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaControl : IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private UIDocument uiDocument;

    [Inject(UxmlName = R.UxmlNames.overviewArea)]
    private VisualElement overviewArea;

    [Inject(UxmlName = R.UxmlNames.overviewAreaWaveform)]
    private VisualElement overviewAreaWaveform;

    [Inject(UxmlName = R.UxmlNames.overviewAreaLabel)]
    private Label overviewAreaLabel;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private Injector injector;

    private OverviewAreaPositionInSongIndicatorControl positionInSongIndicatorControl;
    private OverviewAreaViewportIndicatorControl viewportIndicatorControl;
    private OverviewAreaNoteVisualizer noteVisualizer;
    private OverviewAreaSentenceVisualizer sentenceVisualizer;
    private OverviewAreaIssueVisualizer issueVisualizer;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    public void OnInjectionFinished()
    {
        RegisterPointerEvents();
        positionInSongIndicatorControl = injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaPositionInSongIndicatorControl>();

        viewportIndicatorControl = injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaViewportIndicatorControl>();

        noteVisualizer = injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaNoteVisualizer>();

        issueVisualizer = injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaIssueVisualizer>();

        sentenceVisualizer = injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaSentenceVisualizer>();

        // Create the audio waveform image.
        overviewArea.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            if (!songAudioPlayer.HasAudioClip
                || songAudioPlayer.AudioClip.samples <= 0)
            {
                return;
            }

            using (new DisposableStopwatch($"Created audio waveform in <millis> ms"))
            {
                // For drawing the waveform, the AudioClip must not be streamed. All data must have been fully loaded.
                AudioClip audioClip = AudioManager.Instance.LoadAudioClip(SongMetaUtils.GetAbsoluteSongAudioPath(songMeta), false);
                audioWaveFormVisualization = new AudioWaveFormVisualization(songEditorSceneControl.gameObject, overviewAreaWaveform);
                // Waveform color is same as text color
                audioWaveFormVisualization.waveformColor = overviewAreaLabel.resolvedStyle.color;
                audioWaveFormVisualization.DrawWaveFormMinAndMaxValues(audioClip);
            }
        });
    }

    private void RegisterPointerEvents()
    {
        bool isPointerDown = false;
        overviewArea.RegisterCallback<PointerDownEvent>(evt =>
        {
            isPointerDown = true;
            ScrollToPointer(evt);
        }, TrickleDown.TrickleDown);

        overviewArea.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (isPointerDown)
            {
                ScrollToPointer(evt);
            }
        }, TrickleDown.TrickleDown);

        overviewArea.RegisterCallback<PointerUpEvent>(evt => isPointerDown = false, TrickleDown.TrickleDown);
    }

    private void ScrollToPointer(IPointerEvent evt)
    {
        double xPercent = evt.localPosition.x / overviewArea.contentRect.width;
        double positionInSongInMillis = songAudioPlayer.DurationOfSongInMillis * xPercent;
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
    }
}
