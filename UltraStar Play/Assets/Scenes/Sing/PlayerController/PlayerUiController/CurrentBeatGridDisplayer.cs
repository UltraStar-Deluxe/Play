using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws vertical lines to illustrate where Update is called.
public class CurrentBeatGridDisplayer : MonoBehaviour
{
    public RectTransform linePrefab;

    private readonly List<RectTransform> lines = new List<RectTransform>();

    private SingSceneController singSceneController;
    private Sentence currentSentence;

    public void DisplaySentence(Sentence sentence)
    {
        currentSentence = sentence;
        RemoveAllLines();
    }

    void Awake()
    {
        singSceneController = FindObjectOfType<SingSceneController>();
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

    private void RemoveAllLines()
    {
        foreach (RectTransform rectTransform in lines)
        {
            Destroy(rectTransform.gameObject);
        }
        lines.Clear();
    }

    private void CreateLine(double currentBeat, int sentenceStartBeat, int sentenceEndBeat)
    {
        float x = (float)(currentBeat - sentenceStartBeat) / (sentenceEndBeat - sentenceStartBeat);
        RectTransform line = Instantiate(linePrefab, transform);
        line.anchorMin = new Vector2(x, 0);
        line.anchorMax = new Vector2(x + (2f / 800f), 0.1f);
        line.MoveCornersToAnchors();
        line.GetComponent<Image>().color = Color.red;
        line.GetComponent<Image>().SetAlpha(0.5f);

        lines.Add(line);
    }
}
