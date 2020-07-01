using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Image))]
public class SongRatingImage : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Image image;

    [Inject]
    private SongRating songRating;

    void Start()
    {
        LeanTween.scale(gameObject, Vector3.one, 1f)
            .setFrom(Vector3.one * 0.75f).setEaseSpring();
    }

    public void OnInjectionFinished()
    {
        SongRatingImageHolder[] holders = FindObjectsOfType<SongRatingImageHolder>();
        SongRatingImageHolder holder = holders.Where(it => it.songRatingEnumValue == songRating.EnumValue).FirstOrDefault();
        if (holder != null)
        {
            image.sprite = holder.sprite;
        }
    }
}
