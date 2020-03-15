using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws vertical lines to illustrate where Update is called.
public class CurrentBeatGridDisplayer : MonoBehaviour
{
    private SingSceneController singSceneController;
    private Sentence currentSentence;
    private RectTransform rectTransform;

    public void DisplaySentence(Sentence sentence)
    {
        currentSentence = sentence;
        UiManager.Instance.DestroyAllDebugPoints();
    }

    void Awake()
    {
        singSceneController = FindObjectOfType<SingSceneController>();
        rectTransform = GetComponent<RectTransform>();
    }

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
