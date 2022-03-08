using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

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

    [Inject(UxmlName = R.UxmlNames.videoAreaLabel)]
    private Label videoAreaLabel;

    [Inject(UxmlName = R.UxmlNames.songCoverImage)]
    private VisualElement songCoverImage;

    [Inject(UxmlName = R.UxmlNames.songBackgroundImage)]
    private VisualElement songBackgroundImage;

    [Inject(UxmlName = R.UxmlNames.noVideoImage)]
    private VisualElement noVideoImage;

    [Inject(UxmlName = R.UxmlNames.videoImage)]
    private VisualElement videoImage;

    [Inject(UxmlName = R.UxmlNames.showVideoButton)]
    private Button showVideoButton;

    [Inject(UxmlName = R.UxmlNames.showCoverButton)]
    private Button showCoverButton;

    [Inject(UxmlName = R.UxmlNames.showBackgroundButton)]
    private Button showBackgroundButton;

    private bool isCanceled;
    private float videoGapAtDragStart;

    private GeneralDragControl dragControl;

    public void OnInjectionFinished()
    {
        videoAreaLabel.HideByDisplay();
        if (!songMeta.Video.IsNullOrEmpty())
        {
            ShowVideoImage();
        }
        else if (!songMeta.Cover.IsNullOrEmpty())
        {
            ShowCoverImage();
        }
        else if (!songMeta.Background.IsNullOrEmpty())
        {
            ShowBackgroundImage();
        }

        // Init cover and background image
        if (!songMeta.Background.IsNullOrEmpty())
        {
            ImageManager.LoadSpriteFromUri(SongMetaUtils.GetBackgroundUri(songMeta), sprite => songBackgroundImage.style.backgroundImage = new StyleBackground(sprite));
        }

        // Init cover and background image
        if (!songMeta.Cover.IsNullOrEmpty())
        {
            ImageManager.LoadSpriteFromUri(SongMetaUtils.GetCoverUri(songMeta), sprite => songCoverImage.style.backgroundImage = new StyleBackground(sprite));
        }

        showVideoButton.RegisterCallbackButtonTriggered(() => ShowVideoImage());
        showBackgroundButton.RegisterCallbackButtonTriggered(() => ShowBackgroundImage());
        showCoverButton.RegisterCallbackButtonTriggered(() => ShowCoverImage());

        dragControl = injector
            .WithRootVisualElement(videoImage)
            .CreateAndInject<GeneralDragControl>();
        dragControl.AddListener(this);

        videoImage.RegisterCallback<PointerEnterEvent>(evt => cursorManager.SetCursorHorizontal());
        videoImage.RegisterCallback<PointerLeaveEvent>(evt => cursorManager.SetDefaultCursor());
    }

    private void ShowVideoImage()
    {
        noVideoImage.ShowByDisplay();
        videoImage.ShowByDisplay();
        songBackgroundImage.HideByDisplay();
        songCoverImage.HideByDisplay();
    }

    private void ShowBackgroundImage()
    {
        noVideoImage.HideByDisplay();
        videoImage.HideByDisplay();
        songBackgroundImage.ShowByDisplay();
        songCoverImage.HideByDisplay();
    }

    private void ShowCoverImage()
    {
        noVideoImage.HideByDisplay();
        videoImage.HideByDisplay();
        songBackgroundImage.HideByDisplay();
        songCoverImage.ShowByDisplay();
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
