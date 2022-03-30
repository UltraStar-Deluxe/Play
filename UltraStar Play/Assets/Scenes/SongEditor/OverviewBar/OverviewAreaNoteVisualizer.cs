using System;
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
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(UxmlName = R.UxmlNames.overviewAreaNotes)]
    private VisualElement overviewAreaNotes;

    private DynamicTexture dynamicTexture;

    public void OnInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);

        overviewAreaNotes.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, overviewAreaNotes);
            UpdateNoteOverviewImage();
        });
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (changeEvent is LyricsChangedEvent)
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
        foreach (Voice voice in songMeta.GetVoices())
        {
            Color color = songEditorSceneControl.GetColorForVoice(voice);
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

        int songDurationInMillis = (int)Math.Ceiling(songAudioPlayer.AudioClip.length * 1000);

        // constant offset to
        // (a) ensure that midiNoteRange > 0,
        // (b) have some space to the border of the texture.
        int minMaxOffset = 1;
        int midiNoteMin = notes.Select(note => note.MidiNote).Min() - minMaxOffset;
        int midiNoteMax = notes.Select(note => note.MidiNote).Max() + minMaxOffset;
        int midiNoteRange = midiNoteMax - midiNoteMin;
        foreach (Note note in notes)
        {
            double startMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
            double endMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat);

            int yStart = dynamicTexture.TextureHeight * (note.MidiNote - midiNoteMin) / midiNoteRange;
            int yLength = dynamicTexture.TextureHeight / midiNoteRange * 2;
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
