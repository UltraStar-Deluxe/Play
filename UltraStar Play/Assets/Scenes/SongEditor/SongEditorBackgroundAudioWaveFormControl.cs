using System;
using System.IO;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine;

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
    private AudioManager audioManager;
    
    [Inject]
    private Settings settings;
    
    private int lastNoteAreaMin;
    private int lastNoteAreaWidth;
    private bool isDirty;
    private int lastUpdateAudioWaveformFrameCount;
    
    private AudioWaveFormVisualization audioWaveFormVisualization;
    
    private VisualElement TargetElement => noteAreaWaveform;

    public void OnInjectionFinished()
    {
        noteAreaControl.ViewportEventStream.Subscribe(evt =>
        {
            if (evt.X != lastNoteAreaMin
                || evt.Width != lastNoteAreaWidth)
            {
                TargetElement.HideByVisibility();
                lastNoteAreaMin = evt.X;
                lastNoteAreaWidth = evt.Width;
                isDirty = true;
            }
        });
        
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
            });
        
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource)
            .Subscribe(_ => UpdateAudioWaveForm());
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowAudioWaveformInBackground)
            .Subscribe(_ => UpdateAudioWaveForm());
        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateAudioWaveForm());
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
            || !ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(songMeta.Mp3))
            || !VisualElementUtils.HasGeometry(TargetElement)
            || lastUpdateAudioWaveformFrameCount == Time.frameCount)
        {
            return;
        }
        lastUpdateAudioWaveformFrameCount = Time.frameCount;

        if (audioWaveFormVisualization == null)
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(songEditorSceneControl.gameObject, TargetElement)
            {
                WaveformColor = Colors.darkSlateGrey
            };
        }

        AudioClip audioClip = SongEditorAudioWaveformUtils.GetAudioClipToDrawAudioWaveform(songMeta, audioManager, settings);
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
