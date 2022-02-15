using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorSentenceControl : INeedInjection, IInjectionFinishedListener
{
    private static readonly double handleWidthInPercent = 0.25;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.sentenceImage)]
    private VisualElement sentenceImage;

    [Inject(UxmlName = R.UxmlNames.rightHandle)]
    private VisualElement rightHandle;

    [Inject(UxmlName = R.UxmlNames.leftHandle)]
    private VisualElement leftHandle;

    [Inject(UxmlName = R.UxmlNames.selectionIndicator)]
    private VisualElement selectionIndicator;

    [Inject]
    public Sentence Sentence { get; private set; }

    [Inject(UxmlName = R.UxmlNames.sentenceLabel)]
    private Label sentenceLabel;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private CursorManager cursorManager;

    public bool IsPointerOver { get; private set; }
    public bool IsPointerOverRightHandle { get; private set; }

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    public void OnInjectionFinished()
    {
        rightHandle.HideByDisplay();
        leftHandle.HideByDisplay();
        selectionIndicator.HideByDisplay();
        disposables.Add(uiManager.MousePositionChangeEventStream
            .Subscribe(_ => OnPointerPositionChanged()));

        if (Sentence.Voice != null)
        {
            Color color = songEditorSceneControl.GetColorForVoice(Sentence.Voice);
            SetColor(color);
        }
    }

    public void SetColor(Color color)
    {
        sentenceImage.style.backgroundColor = color;
    }

    public void SetText(string label)
    {
        sentenceLabel.text = label;
    }

    private void OnPointerPositionChanged()
    {
        if (IsPointerOver)
        {
            OnPointerOver();
        }

        UpdateHandles();
    }

    private void OnPointerOver()
    {
        // Vector3 mousePosition = Input.mousePosition;
        // Vector2 localPoint = RectTransform.InverseTransformPoint(mousePosition);
        // float width = RectTransform.rect.width;
        // double xPercent = (localPoint.x + (width / 2)) / width;
        // if (xPercent > (1 - handleWidthInPercent))
        // {
        //     OnPointerOverRightHandle();
        // }
        // else
        // {
        //     OnPointerOverCenter();
        // }
        //
        // UpdateHandles();
    }

    private void OnPointerOverCenter()
    {
        IsPointerOverRightHandle = false;
        cursorManager.SetCursor(ECursor.Grab);
    }

    private void OnPointerOverRightHandle()
    {
        IsPointerOverRightHandle = true;
        cursorManager.SetCursor(ECursor.ArrowsLeftRight);
    }

    private void UpdateHandles()
    {
        bool isRightHandleVisible = IsPointerOverRightHandle;
        rightHandle.SetVisibleByDisplay(isRightHandleVisible);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOver = false;
        IsPointerOverRightHandle = false;
        UpdateHandles();
        cursorManager.SetDefaultCursor();
    }

    public bool IsPositionOverRightHandle(Vector2 position)
    {
        // Vector2 localPoint = RectTransform.InverseTransformPoint(position);
        // float width = RectTransform.rect.width;
        // double xPercent = (localPoint.x + (width / 2)) / width;
        // return (xPercent > (1 - handleWidthInPercent));
        return false;
    }

    public void Dispose()
    {
        disposables.ForEach(it => it.Dispose());
        VisualElement.RemoveFromHierarchy();
    }
}
