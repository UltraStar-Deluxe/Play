using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaSentenceVisualizer : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(UxmlName = R.UxmlNames.overviewAreaSentences)]
    private VisualElement overviewAreaSentences;

    private DynamicTexture dynamicTexture;

    public void OnInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);

        overviewAreaSentences.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, overviewAreaSentences);
            dynamicTexture.backgroundColor = EditorNoteDisplayer.sentenceStartLineColor;
            UpdateSentenceOverviewImage();
        });
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (changeEvent is LyricsChangedEvent)
        {
            return;
        }

        UpdateSentenceOverviewImage();
    }

    private void UpdateSentenceOverviewImage()
    {
        if (dynamicTexture == null)
        {
            return;
        }

        dynamicTexture.ClearTexture();
        foreach (Voice voice in songMeta.GetVoices())
        {
            Color color = songEditorSceneControl.GetColorForVoice(voice);
            DrawAlternatingSentenceBackgrounds(voice);
        }
        dynamicTexture.ApplyTexture();
    }

    private void DrawAlternatingSentenceBackgrounds(Voice voice)
    {
        if (dynamicTexture == null)
        {
            return;
        }

        int songDurationInMillis = (int)Math.Ceiling(songAudioPlayer.AudioClip.length * 1000);

        Color bgColor = dynamicTexture.backgroundColor;
        Color darkBgColor = bgColor.Multiply(0.75f);

        int index = 0;
        foreach (Sentence sentence in voice.Sentences)
        {
            bool isDark = (index % 2 == 0);
            Color finalColor = (isDark) ? darkBgColor : bgColor;

            double startMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MinBeat);
            double endMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MaxBeat);

            int xStart = (int)(dynamicTexture.TextureWidth * startMillis / songDurationInMillis);
            int xEnd = (int)(dynamicTexture.TextureWidth * endMillis / songDurationInMillis);

            if (xEnd < xStart)
            {
                ObjectUtils.Swap(ref xStart, ref xEnd);
            }

            dynamicTexture.DrawRectByCorners(xStart, 0, xEnd, dynamicTexture.TextureHeight, finalColor);

            index++;
        }
    }
}
