using System;
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
    private bool isDirty;
    private int lastUpdateAudioWaveformFrameCount;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private VisualElement TargetElement => noteAreaWaveform;

    public void OnInjectionFinished()
    {
        noteAreaControl.ViewportEventStream
            .Subscribe(evt =>
            {
                if (evt.X != lastNoteAreaMin
                    || evt.Width != lastNoteAreaWidth)
                {
                    TargetElement.HideByVisibility();
                    lastNoteAreaMin = evt.X;
                    lastNoteAreaWidth = evt.Width;
                    isDirty = true;
                }
            })
            .AddTo(gameObject);

        // Update audio wave form when note area was stable for some time.
        noteAreaControl.ViewportEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 800))
            .Subscribe(_ =>
            {
                TargetElement.ShowByVisibility();
                if (isDirty)
                {
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

        if (!songAudioPlayer.IsFullyLoaded
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
            int textureWidth = 1024;
            int textureHeight = 128;
            audioWaveFormVisualization = new AudioWaveFormVisualization(
                songEditorSceneControl.gameObject,
                TargetElement,
                textureWidth,
                textureHeight,
                "song editor background audio visualization");
            audioWaveFormVisualization.WaveformColor = Colors.darkSlateGrey;
        }

        AudioClip audioClip = SongEditorAudioWaveformUtils.GetAudioClipToDrawAudioWaveform(songMeta, settings);
        if (audioClip == null)
        {
            return;
        }

        double minSampleSingleChannel = ((double)noteAreaControl.MinMillisecondsInViewport / 1000) * audioClip.frequency;
        double maxSampleSingleChannel = ((double)noteAreaControl.MaxMillisecondsInViewport / 1000) * audioClip.frequency;
        SongEditorAudioWaveformUtils.DrawAudioWaveform(audioWaveFormVisualization, audioClip, (int)minSampleSingleChannel, (int)maxSampleSingleChannel);

        isDirty = false;
    }
}
