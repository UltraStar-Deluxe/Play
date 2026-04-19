using System;
using System.Linq;
using UniInject;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorDetectedPitchVisualizationControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.noteAreaDetectedPitch)]
    private VisualElement noteAreaDetectedPitch;

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
    
    [Inject]
    private SongEditorLayerManager songEditorLayerManager;
    
    [Inject]
    private SongEditorPitchDetectionControl songEditorPitchDetectionControl;

    private int lastNoteAreaX;
    private int lastNoteAreaY;
    private int lastNoteAreaWidth;
    private int lastNoteAreaHeight;
    private bool isViewportDirty = true;
    private int lastUpdateFrameCount;

    private VisualElement TargetElement => noteAreaDetectedPitch;

    private PitchDetectionResult pitchDetectionResult;

    private int textureWidth = 512;
    private int textureHeight = 256;

    private DynamicTexture dynamicTexture;

    public void OnInjectionFinished()
    {
        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.ShowPitchDetectionResult)
            .Subscribe(_ => noteAreaDetectedPitch.SetVisibleByDisplay(settings.SongEditorSettings.ShowPitchDetectionResult))
            .AddTo(gameObject);

        songEditorPitchDetectionControl.PitchDetectionFinishedEventStream
            .Subscribe(evt =>
            {
                pitchDetectionResult = evt.PitchDetectionResult;
                UpdateVisualization();
            })
            .AddTo(gameObject);

        noteAreaControl.ViewportEventStream
            .Subscribe(evt =>
            {
                if (evt.X != lastNoteAreaX
                    || evt.Y != lastNoteAreaY
                    || evt.Width != lastNoteAreaWidth
                    || evt.Height != lastNoteAreaHeight) 
                {
                    lastNoteAreaX = evt.X;
                    lastNoteAreaY = evt.Y;
                    lastNoteAreaWidth = evt.Width;
                    lastNoteAreaHeight = evt.Height;
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
                    UpdateVisualization();
                }
            })
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource)
            .Subscribe(_ => UpdateVisualization())
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.AudioWaveformSamplesSource)
            .Subscribe(_ => UpdateVisualization())
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowAudioWaveformInBackground)
            .Subscribe(_ => UpdateVisualization())
            .AddTo(gameObject);
        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateVisualization())
            .AddTo(gameObject);
        TargetElement.RegisterCallbackOneShot<GeometryChangedEvent>(
            _ => UpdateVisualization());
        gameObject.OnDestroyAsObservable().Subscribe(_ => Dispose());
    }

    public void UpdateVisualization()
    {
        if (!VisualElementUtils.HasGeometry(TargetElement)
            || noteAreaControl.MinMillisecondsInViewport == noteAreaControl.MaxMillisecondsInViewport
            || !songAudioPlayer.IsFullyLoaded
            || !SongEditorAudioWaveformUtils.IsSupportedAudioFormat(songMeta, settings)
            || lastUpdateFrameCount == Time.frameCount
            || !settings.SongEditorSettings.ShowPitchDetectionResult)
        {
            return;
        }
        lastUpdateFrameCount = Time.frameCount;

        if (dynamicTexture == null)
        {
            dynamicTexture = new DynamicTexture(gameObject, TargetElement, textureWidth, textureHeight, "song editor detected pitch visualization");
            dynamicTexture.backgroundColor = Color.clear;
        }

        dynamicTexture.ClearTexture();
        if (pitchDetectionResult == null)
        {
            dynamicTexture.ApplyTexture();
            return;
        }

        double minMs = noteAreaControl.MinMillisecondsInViewport;
        double maxMs = noteAreaControl.MaxMillisecondsInViewport;
        double durationMs = maxMs - minMs;

        if (durationMs <= 0
            || noteAreaControl.ViewportHeight <= 0)
        {
            return;
        }

        Color color = songEditorLayerManager.GetEnumLayerColor(ESongEditorLayer.PitchDetection);
        foreach (PitchDetectionResultNote note in pitchDetectionResult.Notes)
        {
            if (note.StartInMillis + note.LengthInMillis < minMs || note.StartInMillis > maxMs)
            {
                continue;
            }

            int xStart = (int)((note.StartInMillis - minMs) / durationMs * dynamicTexture.TextureWidth);
            int xEnd = (int)((note.StartInMillis + note.LengthInMillis - minMs) / durationMs * dynamicTexture.TextureWidth);

            xStart = NumberUtils.Limit(xStart, 0, dynamicTexture.TextureWidth - 1);
            xEnd = NumberUtils.Limit(xEnd, 0, dynamicTexture.TextureWidth - 1);

            double heightPercent = noteAreaControl.HeightForSingleNote * 0.1;
            int yOffsetPx = (int)Math.Ceiling(heightPercent * dynamicTexture.TextureHeight * 0.5);
            double yPercent = 1 - noteAreaControl.GetVerticalPositionForMidiNote(note.MidiNote);
            int yPx = (int)(yPercent * dynamicTexture.TextureHeight);
            int yStart = yPx - yOffsetPx;
            int yEnd = yPx + yOffsetPx;

            if (yStart >= 0 && yStart < dynamicTexture.TextureHeight
                && xEnd >= xStart)
            {
                dynamicTexture.DrawRectByCorners(xStart, yStart, xEnd + 1, yEnd, color);
            }
        }

        dynamicTexture.ApplyTexture();
    }
    
    private void Dispose()
    {
        if (dynamicTexture != null)
        {
            dynamicTexture.Dispose();
            dynamicTexture = null;
        }
    }
}
