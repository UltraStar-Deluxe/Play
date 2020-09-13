using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;

public class LineDisplayer : MonoBehaviour
{
    [InjectedInInspector]
    public DynamicallyCreatedImage horizontalGridImage;

    public Color lineColor;

    public int lineHeightInPx = 2;

    private int currentLineCount;
    // Drawing the lines has to be delayed until the texture of the lines has a proper size.
    private int targetLineCount;

    private void Update()
    {
        if (targetLineCount > 0
            && targetLineCount != currentLineCount
            && horizontalGridImage.TextureWidth > 0)
        {
            UpdateLines(targetLineCount);
        }
    }

    public void SetLineCount(int lineCount)
    {
        if (horizontalGridImage.TextureWidth <= 0)
        {
            targetLineCount = lineCount;
        }
        else
        {
            UpdateLines(lineCount);
        }
    }

    private void UpdateLines(int lineCount)
    {
        currentLineCount = lineCount;
        horizontalGridImage.ClearTexture();

        for (int i = 0; i <= lineCount; i++)
        {
            DrawLine(i, lineCount);
        }

        horizontalGridImage.ApplyTexture();
    }

    private void DrawLine(int index, int lineCount)
    {
        double yPercent = (double)index / (double)lineCount;
        int y = (int)((horizontalGridImage.TextureHeight - lineHeightInPx) * yPercent);
        for (int x = 0; x < horizontalGridImage.TextureWidth; x++)
        {
            for (int yOffset = 0; yOffset < lineHeightInPx; yOffset++)
            {
                horizontalGridImage.SetPixel(x, y, lineColor);
                horizontalGridImage.SetPixel(x, y + yOffset, lineColor);
            }
        }
    }
}
