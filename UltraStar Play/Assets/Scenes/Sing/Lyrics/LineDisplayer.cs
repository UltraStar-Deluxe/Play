using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDisplayer : MonoBehaviour
{
    public StarParticle perfectSentenceStarPrefab;
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

    public void CreatePerfectSentenceEffect()
    {
        for (int i = 0; i < 50; i++)
        {
            CreatePerfectSentenceStar();
        }
    }

    private void CreatePerfectSentenceStar()
    {
        StarParticle star = Instantiate(perfectSentenceStarPrefab);
        star.transform.SetParent(transform);
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = UnityEngine.Random.Range(0f, 1f);
        float anchorY = UnityEngine.Random.Range(0f, 1f);
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;

        star.Init();
        star.TargetLifetimeInSeconds = 1f;
        star.StartScale = UnityEngine.Random.Range(0.2f, 0.6f);
        star.TargetScale = 0;
    }
}
