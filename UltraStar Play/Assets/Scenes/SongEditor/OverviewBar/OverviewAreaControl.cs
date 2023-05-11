using System;
using System.IO;
using UniInject;
using UniRx;
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

    [Inject]
    private AudioManager audioManager;

    [Inject]
    private Settings settings;
    
    private OverviewAreaPositionInSongIndicatorControl positionInSongIndicatorControl;
    private OverviewAreaViewportIndicatorControl viewportIndicatorControl;
    private OverviewAreaNoteVisualizer noteVisualizer;
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

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource).Subscribe(_ =>
        {
            UpdateAudioWaveForm();
        });
        
        songAudioPlayer.LoadedEventStream.Subscribe(_ =>
        {
            UpdateAudioWaveForm();
        });
        
        overviewArea.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            UpdateAudioWaveForm();
        });
    }

    public void UpdateAudioWaveForm()
    {
        if (!songAudioPlayer.IsFullyLoaded
            // Must be an audio format. Getting all the samples does not work with video files.
            || !ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(songMeta.Mp3))
            || !VisualElementUtils.HasGeometry(overviewArea))
        {
            return;
        }

        if (audioWaveFormVisualization == null)
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(songEditorSceneControl.gameObject, overviewAreaWaveform)
            {
                // Waveform color is same as text color
                WaveformColor = overviewAreaLabel.resolvedStyle.color
            };
        }

        AudioClip audioClip = SongEditorAudioWaveformUtils.GetAudioClipToDrawAudioWaveform(songMeta, audioManager, settings);
        SongEditorAudioWaveformUtils.DrawAudioWaveform(audioWaveFormVisualization, audioClip);
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
