using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Draws vertical lines to illustrate where Update is called.
public class CurrentBeatGridDisplayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject]
    private PlayerController playerController;

    [Inject]
    private SingSceneController singSceneController;

    [Inject(SearchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private Sentence currentSentence;

    void Update()
    {
        // This script is only for debugging
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
            return;
        }

        if (currentSentence == null)
        {
            return;
        }

        double currentBeat = singSceneController.CurrentBeat;
        CreateLine(currentBeat, currentSentence.MinBeat, currentSentence.MaxBeat);
    }

    public void OnInjectionFinished()
    {
        playerController.EnterSentenceEventStream.Subscribe(enterSentenceEvent =>
        {
            DisplaySentence(enterSentenceEvent.Sentence);
        });
    }

    public void DisplaySentence(Sentence sentence)
    {
        currentSentence = sentence;
        UiManager.Instance.DestroyAllDebugPoints();
    }

    private void CreateLine(double currentBeat, int sentenceStartBeat, int sentenceEndBeat)
    {
        RectTransform line = UiManager.Instance.CreateDebugPoint(rectTransform);
        float x = (float)(currentBeat - sentenceStartBeat) / (sentenceEndBeat - sentenceStartBeat);
        line.anchorMin = new Vector2(x, 0);
        line.anchorMax = new Vector2(x + (2f / 800f), 0.1f);
        line.MoveCornersToAnchors();
        line.GetComponent<Image>().color = Color.red;
        line.GetComponent<Image>().SetAlpha(0.5f);
    }
}
