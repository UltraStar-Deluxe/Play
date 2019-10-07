using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SongRatingImageHolder : MonoBehaviour
{
    public SongRating.ESongRating songRatingEnumValue;
    public Sprite sprite;

    public SongRating SongRating
    {
        get
        {
            return SongRating.Values.Where(it => it.EnumValue == songRatingEnumValue).FirstOrDefault();
        }
    }
}
