using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaControl : INeedInjection, IInjectionFinishedListener
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
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    private AudioWaveFormVisualization audioWaveFormVisualization;
    private ContextMenuControl contextMenuControl;

    public void OnInjectionFinished()
    {
        RegisterPointerEvents();
        injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaPositionIndicatorControl>();

        injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaViewportIndicatorControl>();

        injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaNoteVisualizer>();

        injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<OverviewAreaIssueVisualizer>();

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource)
            .Subscribe(_ => UpdateAudioWaveForm())
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.AudioWaveformSamplesSource)
            .Subscribe(_ => UpdateAudioWaveForm())
            .AddTo(gameObject);

        songAudioPlayer.LoadedEventStream.Subscribe(_ =>
        {
            UpdateAudioWaveForm();
        });

        overviewArea.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            UpdateAudioWaveForm();
        });

        contextMenuControl = injector
            .WithRootVisualElement(overviewArea)
            .CreateAndInject<ContextMenuControl>();
        contextMenuControl.FillContextMenuAction = FillContextMenu;
    }

    private void FillContextMenu(ContextMenuPopupControl popupControl)
    {
        popupControl.AddButton(Translation.Get(R.Messages.songEditor_action_showOriginalAudio),
            () => settings.SongEditorSettings.AudioWaveformSamplesSource = ESongEditorAudioWaveformSamplesSource.OriginalMusic);
        popupControl.AddButton(Translation.Get(R.Messages.songEditor_action_showVocalsAudio),
            () => settings.SongEditorSettings.AudioWaveformSamplesSource = ESongEditorAudioWaveformSamplesSource.Vocals);
        popupControl.AddButton(Translation.Get(R.Messages.songEditor_action_showInstrumentalAudio),
            () => settings.SongEditorSettings.AudioWaveformSamplesSource = ESongEditorAudioWaveformSamplesSource.Instrumental);
        popupControl.AddButton(Translation.Get(R.Messages.songEditor_action_showPlaybackAudio),
            () => settings.SongEditorSettings.AudioWaveformSamplesSource = ESongEditorAudioWaveformSamplesSource.SameAsPlayback);
    }

    public void UpdateAudioWaveForm()
    {
        if (!songAudioPlayer.IsFullyLoaded
            // Must be an audio format. Getting all the samples does not work with video files.
            || !ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(songMeta.Audio))
            || !VisualElementUtils.HasGeometry(overviewArea))
        {
            return;
        }

        if (audioWaveFormVisualization == null)
        {
            int textureWidth = 512;
            int textureHeight = 128;
            audioWaveFormVisualization = new AudioWaveFormVisualization(
                songEditorSceneControl.gameObject,
                overviewAreaWaveform,
                textureWidth,
                textureHeight,
                "song editor overview area audio visualization");
            // Waveform color is same as text color
            audioWaveFormVisualization.WaveformColor = overviewAreaLabel.resolvedStyle.color;
        }

        AudioClip audioClip = SongEditorAudioWaveformUtils.GetAudioClipToDrawAudioWaveform(songMeta, settings);
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
        double positionInMillis = songAudioPlayer.DurationInMillis * xPercent;
        songAudioPlayer.PositionInMillis = positionInMillis;
    }
}
