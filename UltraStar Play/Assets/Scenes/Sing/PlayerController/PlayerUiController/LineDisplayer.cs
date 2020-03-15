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

    public void UpdateLines(int lineCount)
    {
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
