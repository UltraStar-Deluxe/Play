using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LyricsAreaVoiceButton : MonoBehaviour, INeedInjection
{
    public int voiceIndex;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Image image;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    [Inject]
    private LyricsArea lyricsArea;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private SongMeta songMeta;

    void Start()
    {
        if (songMeta.GetVoices().Count <= 1
            || voiceIndex >= songMeta.GetVoices().Count)
        {
            // No need to change voices
            gameObject.SetActive(false);
            return;
        }

        image.color = songEditorSceneController.GetColorForVoice(GetVoice());
        uiText.text = "P" + (voiceIndex + 1);

        button.OnClickAsObservable().Subscribe(_ => lyricsArea.Voice = GetVoice());
    }

    private Voice GetVoice()
    {
        return songMeta.GetVoices()[voiceIndex];
    }
}
