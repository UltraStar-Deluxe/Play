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
            if (targetRouletteItem != null)
            {
                // Cancel old animation
                LeanTween.cancel(gameObject, animIdAnchorMin);
                LeanTween.cancel(gameObject, animIdAnchorMax);
            }

            targetRouletteItem = value;

            if (targetRouletteItem != null)
            {
                transform.SetSiblingIndex(TargetRouletteItem.renderOrder);

                // Animate transition to target position
                float animTimeInSeconds = 0.2f;
                animIdAnchorMin = LeanTween.value(gameObject, RectTransform.anchorMin, TargetRouletteItem.RectTransform.anchorMin, animTimeInSeconds)
                    .setOnUpdate((Vector2 val) => RectTransform.anchorMin = val).id;
                animIdAnchorMax = LeanTween.value(gameObject, RectTransform.anchorMax, TargetRouletteItem.RectTransform.anchorMax, animTimeInSeconds)
                    .setOnUpdate((Vector2 val) => RectTransform.anchorMax = val).id;
            }
        }
    }

    private int animIdAnchorMin;
    private int animIdAnchorMax;

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

    public void Start()
    {
        // Animate to full scale
        LeanTween.scale(RectTransform, Vector3.one, 0.2f);
    }

    public void Update()
    {
        // Destory this item when it does not have a target.
        if (TargetRouletteItem == null)
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
