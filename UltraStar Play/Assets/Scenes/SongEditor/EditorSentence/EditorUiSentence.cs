using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorUiSentence : MonoBehaviour, INeedInjection, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly double handleWidthInPercent = 0.25;

    [InjectedInInspector]
    public Image backgroundImage;

    [InjectedInInspector]
    public RectTransform rightHandle;

    public Sentence Sentence { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponent)]
    public RectTransform RectTransform { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public Text uiText;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private CursorManager cursorManager;

    public bool IsPointerOver { get; private set; }
    public bool IsPointerOverRightHandle { get; private set; }

    public void Start()
    {
        rightHandle.gameObject.SetActive(false);
        uiManager.MousePositionChangeEventStream
            .Subscribe(_ => OnMousePositionChanged())
            .AddTo(gameObject);
    }

    public void Init(Sentence sentence)
    {
        this.Sentence = sentence;

        if (sentence.Voice != null)
        {
            Color color = songEditorSceneControl.GetColorForVoice(sentence.Voice);
            SetColor(color);
        }
    }

    public void SetColor(Color color)
    {
        backgroundImage.color = color;
    }

    public void SetText(string label)
    {
        uiText.text = label;
    }

    private void OnMousePositionChanged()
    {
        if (IsPointerOver)
        {
            OnPointerOver();
        }

        UpdateHandles();
    }

    private void OnPointerOver()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector2 localPoint = RectTransform.InverseTransformPoint(mousePosition);
        float width = RectTransform.rect.width;
        double xPercent = (localPoint.x + (width / 2)) / width;
        if (xPercent > (1 - handleWidthInPercent))
        {
            OnPointerOverRightHandle();
        }
        else
        {
            OnPointerOverCenter();
        }

        UpdateHandles();
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
        if (rightHandle.gameObject.activeSelf != isRightHandleVisible)
        {
            rightHandle.gameObject.SetActive(isRightHandleVisible);
        }
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
        Vector2 localPoint = RectTransform.InverseTransformPoint(position);
        float width = RectTransform.rect.width;
        double xPercent = (localPoint.x + (width / 2)) / width;
        return (xPercent > (1 - handleWidthInPercent));
    }
}
