using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorUiIssue : MonoBehaviour, INeedInjection, IPointerEnterHandler, IPointerExitHandler
{
    public SongIssue SongIssue { get; private set; }

    public bool isPointerOver;

    [InjectedInInspector]
    public Text uiText;

    [InjectedInInspector]
    public Image backgroundImage;

    [InjectedInInspector]
    public Image uiTextBackgroundImage;

    public void Init(SongIssue songIssue)
    {
        this.SongIssue = songIssue;
        uiText.text = SongIssue.Message;
        SetTextVisible(false);
        backgroundImage.color = SongIssueUtils.GetColorForIssue(songIssue);
    }

    private void SetTextVisible(bool isVisible)
    {
        uiText.gameObject.SetActive(isVisible);
        uiTextBackgroundImage.gameObject.SetActive(isVisible);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetTextVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTextVisible(false);
    }
}
