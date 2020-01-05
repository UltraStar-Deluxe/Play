using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

#pragma warning disable CS0649

public class IssueOverviewVisualizer : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [InjectedInInspector]
    public DynamicallyCreatedImage dynImage;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    private IReadOnlyCollection<SongIssue> issues = new List<SongIssue>();

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(ISongMetaChangeEvent changeEvent)
    {
        if (changeEvent is LyricsChangedEvent)
        {
            return;
        }

        issues = SongMetaAnalyzer.AnalyzeIssues(songMeta);
        UpdateIssueOverviewImage();
    }

    void Start()
    {
        UpdateIssueOverviewImage();
    }

    private void UpdateIssueOverviewImage()
    {
        int songDurationInMillis = (int)Math.Ceiling(songAudioPlayer.AudioClip.length * 1000);
        DrawIssues(songDurationInMillis, issues);
    }

    public void DrawIssues(int songDurationInMillis, IEnumerable<SongIssue> issues)
    {
        dynImage.ClearTexture();

        foreach (SongIssue issue in issues)
        {
            DrawIssue(songDurationInMillis, issue);
        }

        dynImage.ApplyTexture();
    }

    private void DrawIssue(int songDurationInMillis, SongIssue issue)
    {
        Color color = SongIssueUtils.GetColorForIssue(issue);

        int startMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, issue.StartBeat);
        int endMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, issue.EndBeat);

        // Use a minimum width of 0.5% such that the issue is not overlooked
        int lengthInMillis = endMillis - startMillis;
        double lengthInPercent = (double)lengthInMillis / songDurationInMillis;
        if (lengthInPercent < 0.005)
        {
            endMillis = (int)(startMillis + songDurationInMillis * 0.005);
        }

        int xStart = (int)(dynImage.TextureWidth * startMillis / songDurationInMillis);
        int xEnd = (int)(dynImage.TextureWidth * endMillis / songDurationInMillis);

        if (xEnd < xStart)
        {
            ObjectUtils.Swap(ref xStart, ref xEnd);
        }

        dynImage.DrawRectByCorners(xStart, 0, xEnd, dynImage.TextureHeight, color);
    }
}
