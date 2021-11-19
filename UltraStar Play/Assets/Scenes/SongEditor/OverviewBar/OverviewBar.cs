using System;
using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;

#pragma warning disable CS0649

public class OverviewBar : MonoBehaviour, IPointerDownHandler, IDragHandler, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private float rectWidth;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    void Start()
    {
        rectWidth = rectTransform.rect.width;
    }

    public void OnDrag(PointerEventData ped)
    {
        ScrollToPointer(ped);
    }

    public void OnPointerDown(PointerEventData ped)
    {
        ScrollToPointer(ped);
    }

    private void ScrollToPointer(PointerEventData ped)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                     ped.position,
                                                                     ped.pressEventCamera,
                                                                     out Vector2 localPoint))
        {
            return;
        }

        double xPercent = (localPoint.x + (rectWidth / 2)) / rectWidth;
        double positionInSongInMillis = songAudioPlayer.DurationOfSongInMillis * xPercent;
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
    }
}
