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

    void Start()
    {
        LeanTween.scale(gameObject, Vector3.one, 1f)
            .setFrom(Vector3.one * 0.75f).setEaseSpring();
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
