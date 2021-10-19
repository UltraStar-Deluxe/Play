using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using System.Globalization;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VideoArea : MonoBehaviour, INeedInjection, IDragListener<GeneralDragEvent>, IPointerEnterHandler, IPointerExitHandler
{
    [InjectedInInspector]
    public GeneralDragHandler videoAreaDragHandler;

    [Inject]
    private CursorManager cursorManager;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SetVideoGapAction setVideoGapAction;

    private bool isCanceled;

    private float videoGapAtDragStart;

    public void Start()
    {
        videoAreaDragHandler.AddListener(this);
    }

    public void CancelDrag()
    {
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    public void OnBeginDrag(GeneralDragEvent dragEvent)
    {
        isCanceled = false;
        if (dragEvent.InputButton != PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        videoGapAtDragStart = songMeta.VideoGap;
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        setVideoGapAction.Execute(GetNewVideoGap(dragEvent));
        cursorManager.SetCursorText("VideoGap: " + songMeta.VideoGap);
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        setVideoGapAction.ExecuteAndNotify(GetNewVideoGap(dragEvent));
        cursorManager.SetCursorTextVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorManager.SetCursorHorizontal();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cursorManager.SetDefaultCursor();
        cursorManager.SetCursorTextVisible(false);
    }

    private float GetNewVideoGap(GeneralDragEvent dragEvent)
    {
        float videoGapDistance = dragEvent.ScreenCoordinateInPercent.Distance.x * 2f;

        // Round to 2 decimal places
        float newVideoGap = (float)Math.Round(videoGapAtDragStart + videoGapDistance, 2);
        return newVideoGap;
    }
}
