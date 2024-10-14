using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorBackgroundAudioWaveFormControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.noteAreaWaveform)]
    private VisualElement noteAreaWaveform;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    private int lastNoteAreaMin;
    private int lastNoteAreaWidth;
    private bool isViewportDirty = true;
    private int lastUpdateAudioWaveformFrameCount;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private VisualElement TargetElement => noteAreaWaveform;

    private AudioClip lastAudioClip;
    private float[] audioWaveFormSamples;

    public void OnInjectionFinished()
    {
        noteAreaControl.ViewportEventStream
            .Subscribe(evt =>
            {
                if (evt.X != lastNoteAreaMin
                    || evt.Width != lastNoteAreaWidth)
                {
                    lastNoteAreaMin = evt.X;
                    lastNoteAreaWidth = evt.Width;
                    isViewportDirty = true;
                }
            })
            .AddTo(gameObject);

        noteAreaControl.ViewportEventStream
            .Subscribe(_ =>
            {
                if (isViewportDirty)
                {
                    isViewportDirty = false;
                    UpdateAudioWaveForm();
                }
            })
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource)
            .Subscribe(_ => UpdateAudioWaveForm())
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.AudioWaveformSamplesSource)
            .Subscribe(_ => UpdateAudioWaveForm())
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowAudioWaveformInBackground)
            .Subscribe(_ => UpdateAudioWaveForm())
            .AddTo(gameObject);
        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateAudioWaveForm())
            .AddTo(gameObject);
        TargetElement.RegisterCallbackOneShot<GeometryChangedEvent>(
            _ => UpdateAudioWaveForm());
    }

    public void UpdateAudioWaveForm()
    {
        if (!settings.SongEditorSettings.ShowAudioWaveformInBackground)
        {
            TargetElement.HideByVisibility();
            return;
        }
        TargetElement.ShowByVisibility();

        if (!VisualElementUtils.HasGeometry(TargetElement)
            || noteAreaControl.MinMillisecondsInViewport == noteAreaControl.MaxMillisecondsInViewport
            || !songAudioPlayer.IsFullyLoaded
            // Must be an audio format. Getting all the samples does not work with video files.
            || !ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(songMeta.Audio))
            || !VisualElementUtils.HasGeometry(TargetElement)
            || lastUpdateAudioWaveformFrameCount == Time.frameCount)
        {
            return;
        }
        lastUpdateAudioWaveformFrameCount = Time.frameCount;

        if (audioWaveFormVisualization == null)
        {
            int textureWidth = 512;
            int textureHeight = 128;
            audioWaveFormVisualization = new AudioWaveFormVisualization(
                songEditorSceneControl.gameObject,
                TargetElement,
                textureWidth,
                textureHeight,
                "song editor background audio visualization");
            audioWaveFormVisualization.WaveformColor = Colors.darkSlateGrey;
            audioWaveFormVisualization.AudioWaveFormCalculator = new PrecalculatingAudioWaveFormCalculator();
        }

        AudioClip audioClip = SongEditorAudioWaveformUtils.GetAudioClipToDrawAudioWaveform(songMeta, settings);
        if (audioClip == null)
        {
            return;
        }

        if (lastAudioClip != audioClip)
        {
            lastAudioClip = audioClip;
            audioWaveFormSamples = AudioUtils.GetAudioSamples(audioClip, 0);
        }

        double minSampleSingleChannel = ((double)noteAreaControl.MinMillisecondsInViewport / 1000) * audioClip.frequency;
        double maxSampleSingleChannel = ((double)noteAreaControl.MaxMillisecondsInViewport / 1000) * audioClip.frequency;
        SongEditorAudioWaveformUtils.DrawAudioWaveform(audioWaveFormVisualization, audioWaveFormSamples, (int)minSampleSingleChannel, (int)maxSampleSingleChannel);
    }
}
