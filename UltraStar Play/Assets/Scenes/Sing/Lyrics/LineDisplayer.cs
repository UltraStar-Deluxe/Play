using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDisplayer : MonoBehaviour
{
    public RectTransform linePrefab;

    public const int LineCount = SentenceDisplayer.NoteLineCount / 2;

    void Start()
    {
        // Create lines
        for (int i = 0; i < LineCount; i++)
        {
            CreateLine(i);
        }
    }

    private void CreateLine(int index)
    {
        RectTransform line = Instantiate(linePrefab);
        line.SetParent(transform);
        // The lines should be the first children,
        // such that they are in the background and the notes are drawn above the lines.
        line.SetSiblingIndex(0);

        Vector2 anchor = new Vector2(0.5f, (float)((double)index / (double)LineCount));
        line.anchorMin = anchor;
        line.anchorMax = anchor;
        line.anchoredPosition = Vector2.zero;
    }
}
