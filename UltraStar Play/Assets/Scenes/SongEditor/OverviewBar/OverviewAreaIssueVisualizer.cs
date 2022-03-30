using System;
using System.Collections.Generic;
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

    [Inject(UxmlName = R.UxmlNames.overviewAreaIssues)]
    private VisualElement overviewAreaIssues;

    private IReadOnlyCollection<SongIssue> issues = new List<SongIssue>();

    private DynamicTexture dynamicTexture;

    public void OnInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(_ =>
        {
            issues = SongMetaAnalyzer.AnalyzeIssues(songMeta);
            UpdateIssueOverviewImage();
        });

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
        foreach (SongIssue issue in issues)
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

        Color color = SongIssueUtils.GetColorForIssue(issue);

        int songDurationInMillis = (int)Math.Ceiling(songAudioPlayer.AudioClip.length * 1000);

        int startMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, issue.StartBeat);
        int endMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, issue.EndBeat);

        // Use a minimum width of 0.5% such that the issue is not overlooked
        int lengthInMillis = endMillis - startMillis;
        float lengthInPercent = (float)lengthInMillis / songDurationInMillis;
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

        dynamicTexture.DrawRectByCorners(xStart, 0, xEnd, (int)(dynamicTexture.TextureHeight * 0.5f), color);
    }
}
