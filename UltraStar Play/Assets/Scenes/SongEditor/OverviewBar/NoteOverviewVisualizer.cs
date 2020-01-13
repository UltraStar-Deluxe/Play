using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

#pragma warning disable CS0649

public class NoteOverviewVisualizer : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [InjectedInInspector]
    public DynamicallyCreatedImage dynImage;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(ISongMetaChangeEvent changeEvent)
    {
        if (changeEvent is LyricsChangedEvent)
        {
            return;
        }

        UpdateNoteOverviewImage();
    }

    void Start()
    {
        UpdateNoteOverviewImage();
    }

    private void UpdateNoteOverviewImage()
    {
        int songDurationInMillis = (int)Math.Ceiling(songAudioPlayer.AudioClip.length * 1000);
        DrawVoices(songDurationInMillis, songMeta);
    }

    public void DrawVoices(int songDurationInMillis, SongMeta songMeta)
    {
        dynImage.ClearTexture();

        foreach (Voice voice in songMeta.GetVoices())
        {
            Color color = songEditorSceneController.GetColorForVoice(voice);
            DrawVoice(songDurationInMillis, songMeta, voice, color);
        }

        dynImage.ApplyTexture();
    }

    private void DrawVoice(int songDurationInMillis, SongMeta songMeta, Voice voice, Color color)
    {
        DrawAlternatingSentenceBackgrounds(songDurationInMillis, songMeta, voice);
        DrawNotes(songDurationInMillis, songMeta, voice, color);
    }

    private void DrawNotes(int songDurationInMillis, SongMeta songMeta, Voice voice, Color color)
    {
        List<Note> notes = voice.Sentences.SelectMany(sentence => sentence.Notes).ToList();
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

            int yStart = dynImage.TextureHeight * (note.MidiNote - midiNoteMin) / midiNoteRange;
            int yLength = dynImage.TextureHeight / midiNoteRange;
            int yEnd = yStart + yLength;
            int xStart = (int)(dynImage.TextureWidth * startMillis / songDurationInMillis);
            int xEnd = (int)(dynImage.TextureWidth * endMillis / songDurationInMillis);
            if (xEnd < xStart)
            {
                ObjectUtils.Swap(ref xStart, ref xEnd);
            }

            dynImage.DrawRectByCorners(xStart, yStart, xEnd, yEnd, color);
        }
    }

    private void DrawAlternatingSentenceBackgrounds(int songDurationInMillis, SongMeta songMeta, Voice voice)
    {
        float f = 0.5f;
        Color bgColor = dynImage.backgroundColor;
        Color darkBgColor = new Color(bgColor.r * f, bgColor.g * f, bgColor.b * f, bgColor.a);

        int index = 0;
        foreach (Sentence sentence in voice.Sentences)
        {
            bool isDark = (index % 2 == 0);
            Color finalColor = (isDark) ? darkBgColor : bgColor;

            double startMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MinBeat);
            double endMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MaxBeat);

            int xStart = (int)(dynImage.TextureWidth * startMillis / songDurationInMillis);
            int xEnd = (int)(dynImage.TextureWidth * endMillis / songDurationInMillis);

            if (xEnd < xStart)
            {
                ObjectUtils.Swap(ref xStart, ref xEnd);
            }

            dynImage.DrawRectByCorners(xStart, 0, xEnd, dynImage.TextureHeight, finalColor);

            index++;
        }
    }
}
