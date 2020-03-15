using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.Globalization;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HighscoreSceneController : MonoBehaviour, INeedInjection, IBinder
{
    [InjectedInInspector]
    public Text titleAndArtistText;
    [InjectedInInspector]
    public Text difficultyText;
    [InjectedInInspector]
    public UiTopEntry[] topEntries;

    [Inject]
    private SceneNavigator sceneNavigator;
    [Inject]
    private Statistics statistics;
    [Inject]
    private I18NManager i18nManager;

    private HighscoreSceneData sceneData;
    private EDifficulty currentDifficulty;

    void Start()
    {
        sceneData = sceneNavigator.GetSceneDataOrThrow<HighscoreSceneData>();
        ShowHighscores(sceneData.SongMeta, sceneData.Difficulty);
    }

    public void FinishScene()
    {
        SongSelectSceneData songSelectSceneData = new SongSelectSceneData();
        songSelectSceneData.SongMeta = sceneData.SongMeta;
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    public void ShowNextDifficulty(int direction)
    {
        int currentDifficutlyIndex = currentDifficulty.GetIndex();
        int nextDifficultyIndex = NumberUtils.Mod(currentDifficutlyIndex + direction, EnumUtils.GetValuesAsList<EDifficulty>().Count);
        EDifficulty nextDifficulty = EnumUtils.GetValuesAsList<EDifficulty>()
            .Where(it => it.GetIndex() == nextDifficultyIndex).FirstOrDefault();
        ShowHighscores(sceneData.SongMeta, nextDifficulty);
    }

    private void ShowHighscores(SongMeta songMeta, EDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        difficultyText.text = i18nManager.GetTranslation(I18NKeys.difficulty) + ": " + difficulty.GetTranslatedName();
        titleAndArtistText.text = $"{songMeta.Title} - {songMeta.Artist}";

        LocalStatistic localStatistic = statistics.GetLocalStats(songMeta);
        List<SongStatistic> songStatistics = localStatistic?.StatsEntries?.SongStatistics?
            .Where(it => it.Difficulty == difficulty).ToList();
        if (songStatistics.IsNullOrEmpty())
        {
            songStatistics = new List<SongStatistic>();
        }
        songStatistics.Sort(new CompareBySongScoreDescending());
        List<SongStatistic> topSongStatistics = songStatistics.Take(topEntries.Length).ToList();
        for (int i = 0; i < topEntries.Length; i++)
        {
            if (i < topSongStatistics.Count)
            {
                ShowHighscore(topEntries[i], topSongStatistics[i], i);
                topEntries[i].gameObject.SetActive(true);
            }
            else
            {
                topEntries[i].gameObject.SetActive(false);
            }
        }
    }

    private void ShowHighscore(UiTopEntry uiTopEntry, SongStatistic songStatistic, int index)
    {
        uiTopEntry.indexText.text = (index + 1).ToString();
        uiTopEntry.playerNameText.text = songStatistic.PlayerName;
        uiTopEntry.scoreText.text = songStatistic.Score.ToString();
        uiTopEntry.dateText.text = songStatistic.DateTime.ToString("d", CultureInfo.CurrentUICulture);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }
}
