using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

public class BeatGridDisplayer : MonoBehaviour
{
    [InjectedInInspector]
    public DynamicallyCreatedImage verticalGridImage;

    public Color lineColor;

    public int lineWidthInPx = 2;

    void Awake()
    {
        // This script is only for debugging
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    public void DisplaySentence(Sentence sentence)
    {
        if (!gameObject.activeInHierarchy || sentence == null)
        {
            return;
        }

        verticalGridImage.ClearTexture();

        DrawLines(sentence.MinBeat, sentence.MaxBeat);

        verticalGridImage.ApplyTexture();
    }

    private void DrawLines(int startBeat, int endBeat)
    {
        int lengthInBeats = endBeat - startBeat;

        for (int i = 0; i <= lengthInBeats; i++)
        {
            DrawLine(i, lengthInBeats);
        }
    }

    private void DrawLine(int i, int lengthInBeats)
    {
        float xPercent = (float)i / lengthInBeats;
        int x = (int)(verticalGridImage.TextureWidth * xPercent);
        for (int xOffset = 0; xOffset < lineWidthInPx; xOffset++)
        {
            for (int y = 0; y < verticalGridImage.TextureHeight; y++)
            {
                verticalGridImage.SetPixel(x + xOffset, y, lineColor);
            }
        }
    }
}
