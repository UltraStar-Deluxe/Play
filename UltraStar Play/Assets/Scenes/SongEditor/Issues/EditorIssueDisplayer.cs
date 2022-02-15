using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorIssueDisplayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [InjectedInInspector]
    public EditorUiIssue issuePrefab;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.noteAreaIssues)]
    private VisualElement noteAreaIssues;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    private IReadOnlyCollection<SongIssue> issues = new List<SongIssue>();

    private float issuePrefabWidthInPixels;

    private ViewportEvent lastViewportEvent;
    private float lastSongMetaBpm;

    public void OnInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(_ => UpdateIssues());
    }

    private void Start()
    {
        issuePrefabWidthInPixels = issuePrefab.GetComponent<RectTransform>().rect.width;

        UpdateIssues();

        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        if (lastViewportEvent == null
            || lastViewportEvent.X != viewportEvent.X
            || lastViewportEvent.Width != viewportEvent.Width
            || songMeta.Bpm != lastSongMetaBpm)
        {
            lastSongMetaBpm = songMeta.Bpm;
            UpdateIssues();
        }
        lastViewportEvent = viewportEvent;
    }

    private void UpdateIssues()
    {
        issues = SongMetaAnalyzer.AnalyzeIssues(songMeta);
        DrawIssues();
    }

    private void DrawIssues()
    {
        noteAreaIssues.Clear();

        foreach (SongIssue issue in issues)
        {
            if (noteAreaControl.IsBeatInViewport(issue.StartBeat))
            {
                CreateUiIssue(issue);
            }
        }
    }

    private void CreateUiIssue(SongIssue issue)
    {
        // EditorUiIssue uiIssue = Instantiate(issuePrefab, noteAreaIssues.transform);
        // injector.Inject(uiIssue);
        // uiIssue.Init(issue);

        // PositionUiIssue(uiIssue, issue.StartBeat);
    }

    private void PositionUiIssue(EditorUiIssue uiIssue, int beat)
    {
        RectTransform uiIssueRectTransform = uiIssue.GetComponent<RectTransform>();

        float widthPercent = issuePrefabWidthInPixels / noteAreaIssues.contentRect.width;
        float xPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(beat);
        uiIssueRectTransform.anchorMin = new Vector2(xPercent, 0);
        uiIssueRectTransform.anchorMax = new Vector2(xPercent + widthPercent, 1);
        uiIssueRectTransform.anchoredPosition = Vector2.zero;
        uiIssueRectTransform.sizeDelta = Vector2.zero;
    }
}
