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
    private SongMeta songMeta;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.noteAreaIssues)]
    private VisualElement noteAreaIssues;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    private IReadOnlyCollection<SongIssue> issues = new List<SongIssue>();

    private ViewportEvent lastViewportEvent;
    private float lastSongMetaBpm;

    public void OnInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(_ => UpdateIssues());
    }

    private void Start()
    {
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
