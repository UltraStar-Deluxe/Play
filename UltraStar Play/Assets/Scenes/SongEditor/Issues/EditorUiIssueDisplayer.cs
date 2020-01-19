using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorUiIssueDisplayer : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [InjectedInInspector]
    public RectTransform uiIssueContainer;

    [InjectedInInspector]
    public EditorUiIssue issuePrefab;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    private IReadOnlyCollection<SongIssue> issues = new List<SongIssue>();

    private float issuePrefabWidthInPixels;

    private ViewportEvent lastViewportEvent;
    private float lastSongMetaBpm;

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    void Start()
    {
        issuePrefabWidthInPixels = issuePrefab.GetComponent<RectTransform>().rect.width;

        UpdateIssues();

        noteArea.ViewportEventStream.Subscribe(OnViewportChanged);
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

    private void OnSongMetaChanged(ISongMetaChangeEvent changeEvent)
    {
        if (changeEvent is LyricsChangedEvent)
        {
            return;
        }

        UpdateIssues();
    }

    private void UpdateIssues()
    {
        issues = SongMetaAnalyzer.AnalyzeIssues(songMeta);
        DrawIssues();
    }

    private void DrawIssues()
    {
        uiIssueContainer.DestroyAllDirectChildren();

        foreach (SongIssue issue in issues)
        {
            if (noteArea.IsBeatInViewport(issue.StartBeat))
            {
                CreateUiIssue(issue);
            }
        }
    }

    private void CreateUiIssue(SongIssue issue)
    {
        EditorUiIssue uiIssue = Instantiate(issuePrefab, uiIssueContainer.transform);
        injector.Inject(uiIssue);
        uiIssue.Init(issue);

        PositionUiIssue(uiIssue, issue.StartBeat);
    }

    private void PositionUiIssue(EditorUiIssue uiIssue, int beat)
    {
        RectTransform uiIssueRectTransform = uiIssue.GetComponent<RectTransform>();

        float xPercent = (float)noteArea.GetHorizontalPositionForBeat(beat);
        float anchorWidth = issuePrefabWidthInPixels / uiIssueContainer.rect.width;
        uiIssueRectTransform.anchorMin = new Vector2(xPercent, 0);
        uiIssueRectTransform.anchorMax = new Vector2(xPercent + anchorWidth, 1);
        uiIssueRectTransform.anchoredPosition = Vector2.zero;
        uiIssueRectTransform.sizeDelta = Vector2.zero;
    }
}
