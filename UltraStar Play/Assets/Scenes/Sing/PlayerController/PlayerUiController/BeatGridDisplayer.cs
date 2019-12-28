using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatGridDisplayer : MonoBehaviour
{
    public RectTransform linePrefab;

    private readonly List<RectTransform> lines = new List<RectTransform>();

    public void DisplaySentence(Sentence sentence)
    {
        if (!enabled || sentence == null)
        {
            return;
        }

        RemoveAllLines();

        CreateLines(sentence.MinBeat, sentence.MaxBeat);
    }

    private void RemoveAllLines()
    {
        foreach (RectTransform rectTransform in lines)
        {
            Destroy(rectTransform.gameObject);
        }
        lines.Clear();
    }

    private void CreateLines(int StartBeat, int EndBeat)
    {
        int lengthInBeats = EndBeat - StartBeat;

        for (int i = 0; i <= lengthInBeats; i++)
        {
            CreateLine(i, lengthInBeats);
        }
    }

    private void CreateLine(int i, int lengthInBeats)
    {
        RectTransform line = Instantiate(linePrefab, transform);
        float x = (float)i / lengthInBeats;
        line.anchorMin = new Vector2(x, 0);
        line.anchorMax = new Vector2(x + (2f / 800f), 1);
        line.MoveCornersToAnchors();
        line.GetComponent<Image>().SetAlpha(0.25f);

        lines.Add(line);
    }
}
