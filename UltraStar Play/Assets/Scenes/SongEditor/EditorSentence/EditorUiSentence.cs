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

public class EditorUiSentence : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Image backgroundImage;

    public Sentence Sentence { get; private set; }

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    public void Init(Sentence sentence)
    {
        this.Sentence = sentence;

        if (sentence.Voice != null)
        {
            Color color = songEditorSceneController.GetColorForVoice(sentence.Voice);
            SetColor(color);
        }
    }

    private void SetColor(Color color)
    {
        backgroundImage.color = color;
    }
}
