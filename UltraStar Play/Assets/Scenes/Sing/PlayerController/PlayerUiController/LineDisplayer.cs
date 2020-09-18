using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UnityEngine.UI;

public class LineDisplayer : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public DynamicallyCreatedImage horizontalGridImage;

    public Color lineColor;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private int currentLineCount;
    // Drawing the lines has to be delayed until the texture of the lines has a proper size.
    private int targetLineCount;

    private void Update()
    {
        if (targetLineCount > 0
            && targetLineCount != currentLineCount
            && rectTransform.rect.width > 0
            && rectTransform.rect.height > 0)
        {
            UpdateLines(targetLineCount);
        }
    }

    public void SetTargetLineCount(int lineCount)
    {
        targetLineCount = lineCount;
    }

    private void UpdateLines(int lineCount)
    {
        currentLineCount = lineCount;
        horizontalGridImage.ClearTexture();

        for (int i = 1; i <= lineCount; i++)
        {
            DrawLine(i, lineCount);
        }

        horizontalGridImage.ApplyTexture();
    }

    private void DrawLine(int index, int lineCount)
    {
        float yPercent = (float)index / (float)lineCount;
        int y = (int)(horizontalGridImage.TextureHeight * yPercent);
        for (int x = 0; x < horizontalGridImage.TextureWidth; x++)
        {
            horizontalGridImage.SetPixel(x, y, lineColor);
        }
    }
}
