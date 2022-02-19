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
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VideoAreaControl : INeedInjection, IInjectionFinishedListener, IDragListener<GeneralDragEvent>
{
    [Inject]
    private CursorManager cursorManager;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SetVideoGapAction setVideoGapAction;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.videoArea)]
    private VisualElement videoArea;

    [Inject(UxmlName = R.UxmlNames.videoAreaLabel)]
    private Label videoAreaLabel;

    private bool isCanceled;
    private float videoGapAtDragStart;

    private GeneralDragControl dragControl;

    public void OnInjectionFinished()
    {
        videoAreaLabel.HideByDisplay();

        dragControl = injector
            .WithRootVisualElement(videoArea)
            .CreateAndInject<GeneralDragControl>();
        dragControl.AddListener(this);

        videoArea.RegisterCallback<PointerEnterEvent>(evt => cursorManager.SetCursorHorizontal());
        videoArea.RegisterCallback<PointerLeaveEvent>(evt => cursorManager.SetDefaultCursor());
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
        if (dragEvent.InputButton != (int)PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        videoGapAtDragStart = songMeta.VideoGap;
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        setVideoGapAction.Execute(GetNewVideoGap(dragEvent));
        videoAreaLabel.ShowByDisplay();
        videoAreaLabel.text = $"VideoGap: {songMeta.VideoGap}";
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        setVideoGapAction.ExecuteAndNotify(GetNewVideoGap(dragEvent));
        videoAreaLabel.HideByDisplay();
    }

    private float GetNewVideoGap(GeneralDragEvent dragEvent)
    {
        float videoGapDistance = dragEvent.ScreenCoordinateInPercent.Distance.x * 2f;

        // Round to 2 decimal places
        float newVideoGap = (float)Math.Round(videoGapAtDragStart + videoGapDistance, 2);
        return newVideoGap;
    }
}
