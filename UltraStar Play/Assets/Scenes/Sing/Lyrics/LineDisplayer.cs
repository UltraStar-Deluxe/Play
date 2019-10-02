using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDisplayer : MonoBehaviour
{
    public RectTransform linePrefab;

    public void Init(int lineCount)
    {
        for (int i = 0; i < lineCount; i++)
        {
            CreateLine(i, lineCount);
        }
    }

    private void CreateLine(int index, int lineCount)
    {
        RectTransform line = Instantiate(linePrefab);
        line.SetParent(transform);
        // The lines should be the first children,
        // such that they are in the background and the notes are drawn above the lines.
        line.SetSiblingIndex(0);

        double indexPercentage = (double)index / (double)lineCount;
        Vector2 anchor = new Vector2(0.5f, (float)indexPercentage);
        line.anchorMin = anchor;
        line.anchorMax = anchor;
        line.anchoredPosition = Vector2.zero;
    }
}
