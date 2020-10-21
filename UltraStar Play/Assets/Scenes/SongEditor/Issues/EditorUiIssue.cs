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

public class EditorUiIssue : MonoBehaviour, INeedInjection
{
    public SongIssue SongIssue { get; private set; }

    public bool isPointerOver;

    [InjectedInInspector]
    public Image backgroundImage;

    [InjectedInInspector]
    public Image uiTextBackgroundImage;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private TooltipHandler tooltipHandler;

    public void Init(SongIssue songIssue)
    {
        this.SongIssue = songIssue;
        backgroundImage.color = SongIssueUtils.GetColorForIssue(songIssue);
        tooltipHandler.tooltipText = songIssue.Message;
    }
}
