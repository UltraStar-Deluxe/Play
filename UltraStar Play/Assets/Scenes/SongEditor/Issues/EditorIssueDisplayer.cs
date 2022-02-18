using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorIssueDisplayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [InjectedInInspector]
    public VisualTreeAsset editorIssueUi;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongEditorIssueAnalyzerControl issueAnalyzerControl;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMeta songMeta;

    [Inject(UxmlName = R.UxmlNames.noteAreaIssues)]
    private VisualElement noteAreaIssues;

    private ViewportEvent lastViewportEvent;
    private float lastSongMetaBpm;

    private IReadOnlyCollection<SongIssue> lastIssues;

    public void OnInjectionFinished()
    {
        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);
        issueAnalyzerControl.IssuesEventStream.Subscribe(issues => DrawIssues(issues));
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        if (lastViewportEvent == null
            || lastViewportEvent.X != viewportEvent.X
            || lastViewportEvent.Width != viewportEvent.Width
            || !songMeta.Bpm.NearlyEquals(lastSongMetaBpm, 0.01f))
        {
            lastSongMetaBpm = songMeta.Bpm;
            DrawIssues(lastIssues);
        }
        lastViewportEvent = viewportEvent;
    }

    private void DrawIssues(IReadOnlyCollection<SongIssue> issues)
    {
        lastIssues = issues;
        noteAreaIssues.Clear();

        if (issues.IsNullOrEmpty())
        {
            return;
        }

        issues.Where(issue => noteAreaControl.IsBeatInViewport(issue.StartBeat))
            .ForEach(issue => CreateUiIssue(issue));
    }

    private void CreateUiIssue(SongIssue issue)
    {
        VisualElement visualElement = editorIssueUi.CloneTree().Children().First();
        EditorIssueControl editorIssueControl = injector
            .WithRootVisualElement(visualElement)
            .WithBindingForInstance(issue)
            .CreateAndInject<EditorIssueControl>();
        noteAreaIssues.Add(visualElement);

        editorIssueControl.VisualElement.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            PositionEditorIssueControl(editorIssueControl.VisualElement, issue.StartBeat);
        });
    }

    private void PositionEditorIssueControl(VisualElement visualElement, int beat)
    {
        float widthPercent = visualElement.contentRect.width / noteAreaIssues.contentRect.width;
        float xPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(beat);
        visualElement.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
        visualElement.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
    }
}
