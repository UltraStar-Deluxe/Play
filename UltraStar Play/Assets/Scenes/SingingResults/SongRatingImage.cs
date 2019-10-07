using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SongRatingImage : MonoBehaviour
{
    private Image image;

    void OnEnable()
    {
        image = GetComponent<Image>();
    }

    public void SetSongRating(SongRating rating)
    {
        SongRatingImageHolder[] holders = FindObjectsOfType<SongRatingImageHolder>();
        SongRatingImageHolder holder = holders.Where(it => it.songRatingEnumValue == rating.EnumValue).FirstOrDefault();
        if (holder != null)
        {
            image.sprite = holder.sprite;
        }
    }
}
