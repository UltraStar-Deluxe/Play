using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaNoteVisualizer : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject(UxmlName = R.UxmlNames.overviewAreaNotes)]
    private VisualElement overviewAreaNotes;

    private DynamicTexture dynamicTexture;

    public void OnInjectionFinished()
    {
        songMetaChangedEventStream.Subscribe(OnSongMetaChanged);

        songAudioPlayer.LoadedEventStream.Subscribe(_ =>
        {
            UpdateNoteOverviewImage();
        });

        overviewAreaNotes.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, overviewAreaNotes);
            UpdateNoteOverviewImage();
        });
    }

    private void OnSongMetaChanged(SongMetaChangedEvent changedEvent)
    {
        if (changedEvent is LyricsChangedEvent)
        {
            return;
        }

        UpdateNoteOverviewImage();
    }

    private void UpdateNoteOverviewImage()
    {
        if (dynamicTexture == null)
        {
            return;
        }

        dynamicTexture.ClearTexture();
        foreach (Voice voice in songMeta.Voices)
        {
            Color color = songEditorLayerManager.GetVoiceLayerColor(voice.Id);
            DrawNotes(voice, color);
        }
        dynamicTexture.ApplyTexture();
    }

    private void DrawNotes(Voice voice, Color color)
    {
        if (dynamicTexture == null)
        {
            return;
        }

        List<Note> notes = voice.Sentences.SelectMany(sentence => sentence.Notes).ToList();
        if (notes.IsNullOrEmpty())
        {
            return;
        }

        int songDurationInMillis = (int)songAudioPlayer.DurationInMillis;

        // constant offset to
        // (a) ensure that midiNoteRange > 0,
        // (b) have some space to the border of the texture.
        int minMaxOffset = 1;
        int midiNoteMin = notes.Select(note => note.MidiNote).Min() - minMaxOffset;
        int midiNoteMax = notes.Select(note => note.MidiNote).Max() + minMaxOffset;
        int midiNoteRange = midiNoteMax - midiNoteMin;
        foreach (Note note in notes)
        {
            double startMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.StartBeat);
            startMillis = NumberUtils.Limit(startMillis, 0, songDurationInMillis);
            double endMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.EndBeat);
            startMillis = NumberUtils.Limit(startMillis, 0, songDurationInMillis);

            int yStart = dynamicTexture.TextureHeight * (note.MidiNote - midiNoteMin) / midiNoteRange;
            int yLength = dynamicTexture.TextureHeight / midiNoteRange * 2;

            int minHeightInPx = 5;
            yLength = NumberUtils.Limit(yLength, minHeightInPx, dynamicTexture.TextureHeight);

            int yEnd = yStart + yLength;
            int xStart = (int)(dynamicTexture.TextureWidth * startMillis / songDurationInMillis);
            int xEnd = (int)(dynamicTexture.TextureWidth * endMillis / songDurationInMillis);
            if (xEnd < xStart)
            {
                ObjectUtils.Swap(ref xStart, ref xEnd);
            }

            xEnd = xEnd < dynamicTexture.TextureWidth
                ? xEnd
                : dynamicTexture.TextureWidth - 1;
            yEnd = yEnd < dynamicTexture.TextureHeight
                ? yEnd
                : dynamicTexture.TextureHeight - 1;
            dynamicTexture.DrawRectByCorners(xStart, yStart, xEnd, yEnd, color);
        }
    }
}
