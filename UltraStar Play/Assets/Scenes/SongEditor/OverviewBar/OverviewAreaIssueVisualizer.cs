using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaIssueVisualizer : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongEditorIssueAnalyzerControl issueAnalyzerControl;

    [Inject(UxmlName = R.UxmlNames.overviewAreaIssues)]
    private VisualElement overviewAreaIssues;

    private DynamicTexture dynamicTexture;

    public void OnInjectionFinished()
    {
        songAudioPlayer.LoadedEventStream.Subscribe(_ => UpdateIssueOverviewImage());
        issueAnalyzerControl.IssuesEventStream.Subscribe(_ => UpdateIssueOverviewImage());

        overviewAreaIssues.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, overviewAreaIssues);
            UpdateIssueOverviewImage();
        });
    }

    private void UpdateIssueOverviewImage()
    {
        if (dynamicTexture == null)
        {
            return;
        }

        dynamicTexture.ClearTexture();
        foreach (SongIssue issue in issueAnalyzerControl.Issues)
        {
            DrawIssue(issue);
        }
        dynamicTexture.ApplyTexture();
    }

    private void DrawIssue(SongIssue issue)
    {
        if (dynamicTexture == null)
        {
            return;
        }

        int songDurationInMillis = (int)songAudioPlayer.DurationInMillis;
        if (songDurationInMillis <= 0)
        {
            // Song is not loaded yet
            return;
        }

        int startMillis = (int)SongMetaBpmUtils.BeatsToMillis(songMeta, issue.StartBeat);
        int endMillis = (int)SongMetaBpmUtils.BeatsToMillis(songMeta, issue.EndBeat);

        // Use a minimum width of 0.5% such that the issue is not overlooked
        int lengthInMillis = endMillis - startMillis;
        double lengthInPercent = (double)lengthInMillis / songDurationInMillis;
        if (lengthInPercent < 0.005)
        {
            endMillis = (int)(startMillis + songDurationInMillis * 0.005);
        }

        int xStart = (int)(dynamicTexture.TextureWidth * startMillis / songDurationInMillis);
        int xEnd = (int)(dynamicTexture.TextureWidth * endMillis / songDurationInMillis);

        if (xEnd < xStart)
        {
            ObjectUtils.Swap(ref xStart, ref xEnd);
        }

        Color color = issue.Severity == ESongIssueSeverity.Error ? Colors.red : Colors.yellow;
        dynamicTexture.DrawRectByCorners(xStart, 0, xEnd, (int)(dynamicTexture.TextureHeight * 0.25f), color);
    }
}
