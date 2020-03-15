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

public class SongSelectHighscoreDisplayer : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Text highscoreLocalPlayerText;
    [InjectedInInspector]
    public Text highscoreLocalScoreText;
    [InjectedInInspector]
    public Text highscoreWebPlayerText;
    [InjectedInInspector]
    public Text highscoreWebScoreText;

    [Inject]
    private Statistics statistics;

    [Inject]
    private SongRouletteController songRouletteController;

    void Start()
    {
        songRouletteController.Selection.Subscribe(songSelection => UpdateHighscoreText(songSelection.SongMeta));
    }

    private void UpdateHighscoreText(SongMeta selectedSong)
    {
        ResetHighscoreText();
        if (selectedSong == null)
        {
            return;
        }

        // Display local highscore
        LocalStatistic localStats = statistics.GetLocalStats(selectedSong);
        if (localStats != null)
        {
            SongStatistic localTopScore = localStats.StatsEntries.TopScore;
            if (localTopScore != null)
            {
                highscoreLocalPlayerText.text = localTopScore.PlayerName;
                highscoreLocalScoreText.text = localTopScore.Score.ToString();
            }
        }

        // Display web highscore
        WebStatistic webStats = statistics.GetWebStats(selectedSong);
        if (webStats != null)
        {
            SongStatistic webTopScore = webStats.StatsEntries.TopScore;
            if (webTopScore != null)
            {
                highscoreWebPlayerText.text = webTopScore.PlayerName;
                highscoreWebScoreText.text = webTopScore.Score.ToString();
            }
        }
    }

    private void ResetHighscoreText()
    {
        highscoreLocalPlayerText.text = "";
        highscoreLocalScoreText.text = "0";
        highscoreWebPlayerText.text = "";
        highscoreWebScoreText.text = "0";
    }
}
