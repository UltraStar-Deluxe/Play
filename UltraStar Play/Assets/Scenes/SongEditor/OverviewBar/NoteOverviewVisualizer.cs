using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

[RequireComponent(typeof(RawImage))]
public class NoteOverviewVisualizer : MonoBehaviour, INeedInjection
{
    public Color backgroundColor = new Color(0, 0, 0, 0);

    private Color[] voiceColors = { Colors.crimson, Colors.forestGreen, Colors.dodgerBlue,
                                    Colors.gold, Colors.greenYellow, Colors.salmon, Colors.violet };

    private Color[] blank; // blank image array (background color in every pixel)
    private Texture2D texture;

    private RawImage rawImage;
    private RectTransform rectTransform;

    private int textureWidth = 256;
    private int textureHeight = 256;

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
        CreateTexture();
    }

    void Start()
    {
        int songDurationInMillis = (int)Math.Ceiling(songAudioPlayer.AudioClip.length * 1000);
        DrawVoices(songDurationInMillis, songMeta, voices);
    }

    public void DrawVoices(int songDurationInMillis, SongMeta songMeta, List<Voice> voices)
    {
        ClearTexture();

        int voiceIndex = 0;
        foreach (Voice voice in voices)
        {
            Color color = voiceColors[voiceIndex];
            DrawVoice(songDurationInMillis, songMeta, voice, color);
        }

        // upload to the graphics card 
        texture.Apply();
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

            int yStart = textureHeight * (note.MidiNote - midiNoteMin) / midiNoteRange;
            int yLength = textureHeight / midiNoteRange;
            int xStart = (int)(textureWidth * startMillis / songDurationInMillis);
            int xEnd = (int)(textureWidth * startMillis / songDurationInMillis);
            if (xEnd < xStart)
            {
                Swap(ref xStart, ref xEnd);
            }

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yStart + yLength; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private void DrawAlternatingSentenceBackgrounds(int songDurationInMillis, SongMeta songMeta, Voice voice)
    {
        float f = 0.5f;
        Color bgColor = backgroundColor;
        Color darkBgColor = new Color(bgColor.r * f, bgColor.g * f, bgColor.b * f, bgColor.a);

        int index = 0;
        foreach (Sentence sentence in voice.Sentences)
        {
            bool isDark = (index % 2 == 0);
            Color finalColor = (isDark) ? darkBgColor : bgColor;

            double startMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.StartBeat);
            double endMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.EndBeat);

            int xStart = (int)(textureWidth * startMillis / songDurationInMillis);
            int xEnd = (int)(textureWidth * endMillis / songDurationInMillis);

            if (xEnd < xStart)
            {
                Swap(ref xStart, ref xEnd);
            }

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    texture.SetPixel(x, y, finalColor);
                }
            }

            index++;
        }
    }

    private void Swap(ref int xStart, ref int xEnd)
    {
        int tmp = xStart;
        xStart = xEnd;
        xEnd = tmp;
    }

    private void CreateTexture()
    {
        // The size of the RectTransform can be zero in the first frame, when inside a layout group.
        // See https://forum.unity.com/threads/solved-cant-get-the-rect-width-rect-height-of-an-element-when-using-layouts.377953/
        if (rectTransform.rect.width != 0)
        {
            textureWidth = (int)rectTransform.rect.width;
        }
        if (rectTransform.rect.height != 0)
        {
            textureHeight = (int)rectTransform.rect.height;
        }

        // create the texture and assign to the rawImage
        texture = new Texture2D(textureWidth, textureHeight);
        rawImage.texture = texture;

        // create a 'blank screen' image 
        blank = new Color[textureWidth * textureHeight];
        for (int i = 0; i < blank.Length; i++)
        {
            blank[i] = backgroundColor;
        }

        // reset the texture to the background color
        ClearTexture();
    }

    private void ClearTexture()
    {
        texture.SetPixels(blank);
    }
}
