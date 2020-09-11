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

public class SongSelectInfoTextDisplayer : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Text highscoreLocalPlayerText;
    [InjectedInInspector]
    public Text highscoreLocalScoreText;

    [InjectedInInspector]
    public Text highscoreWebPlayerText;
    [InjectedInInspector]
    public Text highscoreWebScoreText;

    [InjectedInInspector]
    public Text countStartedFinishedText;

    [Inject]
    private Statistics statistics;

    [Inject]
    private SongRouletteController songRouletteController;

    void Start()
    {
        songRouletteController.Selection.Subscribe(songSelection => UpdateSongInfoText(songSelection.SongMeta));
    }

    private void UpdateSongInfoText(SongMeta selectedSong)
    {
        ResetInfoText();
        if (selectedSong == null)
        {
            return;
        }

        LocalStatistic localStats = statistics.GetLocalStats(selectedSong);
        if (localStats != null)
        {
            // Display local highscore
            SongStatistic localTopScore = localStats.StatsEntries.TopScore;
            if (localTopScore != null)
            {
                SetLocalHighscoreText(localTopScore.PlayerName, localTopScore.Score);
            }

            // Display count started/finished
            SetStartedFinishedText(localStats.TimesStarted, localStats.TimesFinished);
        }

        // Display web highscore
        WebStatistic webStats = statistics.GetWebStats(selectedSong);
        if (webStats != null)
        {
            SongStatistic webTopScore = webStats.StatsEntries.TopScore;
            if (webTopScore != null)
            {
                SetWebHighscoreText(webTopScore.PlayerName, webTopScore.Score);
            }
        }
    }

    private void SetLocalHighscoreText(string playerName, int score)
    {
        highscoreLocalPlayerText.text = playerName;
        highscoreLocalScoreText.text = score.ToString();
    }

    private void SetWebHighscoreText(string playerName, int score)
    {
        highscoreWebPlayerText.text = playerName;
        highscoreWebScoreText.text = score.ToString();
    }

    private void SetStartedFinishedText(int started, int finished)
    {
        countStartedFinishedText.text = started + "/" + finished;
    }

    private void ResetInfoText()
    {
        SetLocalHighscoreText("", 0);
        SetWebHighscoreText("", 0);
        SetStartedFinishedText(0, 0);
    }
}
