using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SongRouletteItem : MonoBehaviour
{
    private SongMeta songMeta;
    public SongMeta SongMeta
    {
        get
        {
            return songMeta;
        }
        set
        {
            songMeta = value;
            UpdateCover(songMeta);
        }
    }

    private RouletteItemPlaceholder targetRouletteItem;
    public RouletteItemPlaceholder TargetRouletteItem
    {
        get
        {
            return targetRouletteItem;
        }
        set
        {
            targetRouletteItem = value;
            if (targetRouletteItem != null)
            {
                startAnchorMin = RectTransform.anchorMin;
                startAnchorMax = RectTransform.anchorMax;
                AnimTime = 0;
            }
        }
    }

    private Vector2 startAnchorMin;
    private Vector2 startAnchorMax;

    public Vector3 Scale
    {
        get
        {
            return RectTransform.localScale;
        }
        set
        {
            RectTransform.localScale = value;
        }
    }

    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }

    public float AnimTime { get; set; }
    public float ScaleTime { get; set; }

    void Update()
    {
        ScaleTime += 10 * Time.deltaTime;
        if (ScaleTime >= 1)
        {
            ScaleTime = 1;
        }
        Scale = new Vector2(ScaleTime, ScaleTime);

        AnimTime += 10 * Time.deltaTime;
        if (AnimTime >= 1)
        {
            AnimTime = 1;
        }

        if (TargetRouletteItem != null)
        {
            transform.SetSiblingIndex(TargetRouletteItem.renderOrder);
            RectTransform.anchorMin = Vector2.Lerp(startAnchorMin, TargetRouletteItem.RectTransform.anchorMin, AnimTime);
            RectTransform.anchorMax = Vector2.Lerp(startAnchorMax, TargetRouletteItem.RectTransform.anchorMax, AnimTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void UpdateCover(SongMeta songMeta)
    {
        Image image = GetComponent<Image>();
        string coverPath = songMeta.Directory + Path.DirectorySeparatorChar + songMeta.Cover;
        if (File.Exists(coverPath))
        {
            Sprite sprite = ImageManager.LoadSprite(coverPath);
            image.sprite = sprite;
        }
        else
        {
            Debug.Log("Cover does not exist: " + coverPath);
        }
    }
}
