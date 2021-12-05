using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using System.Globalization;
using ProTrans;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HighscoreSceneUiControl : MonoBehaviour, INeedInjection, IBinder, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.hiddenContinueButton)]
    private Button hiddenContinueButton;

    [Inject(UxmlName = R.UxmlNames.nextItemButton)]
    private Button nextItemButton;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.sceneSubtitle)]
    private Label titleAndArtistText;

    [Inject(UxmlName = R.UxmlNames.difficultyLabel)]
    private Label difficultyText;

    // TODO: Make injectable by UniInject
    private List<VisualElement> highscoreEntries;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Statistics statistics;

    [Inject]
    private UIDocument uiDocument;

    private HighscoreSceneData sceneData;
    private EDifficulty currentDifficulty;

    void Start()
    {
        sceneData = sceneNavigator.GetSceneDataOrThrow<HighscoreSceneData>();
        highscoreEntries = uiDocument.rootVisualElement.Query<VisualElement>(R.UxmlNames.highscoreEntry)
            .ToList();
        continueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        hiddenContinueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        nextItemButton.RegisterCallbackButtonTriggered(() => ShowNextDifficulty(1));
        ShowHighscores(sceneData.SongMeta, sceneData.Difficulty);

        continueButton.Focus();
    }

    public void FinishScene()
    {
        SongSelectSceneData songSelectSceneData = new SongSelectSceneData();
        songSelectSceneData.SongMeta = sceneData.SongMeta;
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    public void ShowNextDifficulty(int direction)
    {
        EDifficulty nextDifficulty = GetNextDifficulty(currentDifficulty, direction);
        ShowHighscores(sceneData.SongMeta, nextDifficulty);
    }

    private void ShowHighscores(SongMeta songMeta, EDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        difficultyText.text = TranslationManager.GetTranslation(R.Messages.difficulty) + ": " + difficulty.GetTranslatedName();
        titleAndArtistText.text = $"{songMeta.Title} - {songMeta.Artist}";

        LocalStatistic localStatistic = statistics.GetLocalStats(songMeta);
        List<SongStatistic> songStatistics = localStatistic?.StatsEntries?.SongStatistics?
            .Where(it => it.Difficulty == difficulty).ToList();
        if (songStatistics.IsNullOrEmpty())
        {
            songStatistics = new List<SongStatistic>();
        }
        songStatistics.Sort(new CompareBySongScoreDescending());
        List<SongStatistic> topSongStatistics = songStatistics.Take(highscoreEntries.Count).ToList();
        for (int i = 0; i < highscoreEntries.Count; i++)
        {
            if (i < topSongStatistics.Count)
            {
                highscoreEntries[i].Show();
                FillHighscoreEntry(highscoreEntries[i], topSongStatistics[i], i);
            }
            else
            {
                highscoreEntries[i].Hide();
            }
        }

        // update "next difficulty button" text
        UpdateTranslation();
    }

    private void FillHighscoreEntry(VisualElement highscoreEntry, SongStatistic songStatistic, int index)
    {
        highscoreEntry.Q<Label>(R.UxmlNames.posLabel).text = (index + 1).ToString();
        highscoreEntry.Q<Label>(R.UxmlNames.playerNameLabel).text = songStatistic.PlayerName;
        highscoreEntry.Q<Label>(R.UxmlNames.scoreLabel).text = songStatistic.Score.ToString();
        highscoreEntry.Q<Label>(R.UxmlNames.dateLabel).text = songStatistic.DateTime.ToString("d", CultureInfo.CurrentUICulture);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.highscoreScene_title);
        continueButton.text = TranslationManager.GetTranslation(R.Messages.continue_);

        EDifficulty nextDifficulty = GetNextDifficulty(currentDifficulty, 1);
        nextItemButton.Q<Label>().text = nextDifficulty.GetTranslatedName();
    }

    private EDifficulty GetNextDifficulty(EDifficulty difficulty, int direction)
    {
        int currentDifficultyIndex = difficulty.GetIndex();
        int nextDifficultyIndex = NumberUtils.Mod(currentDifficultyIndex + direction, EnumUtils.GetValuesAsList<EDifficulty>().Count);
        EDifficulty nextDifficulty = EnumUtils.GetValuesAsList<EDifficulty>()
            .FirstOrDefault(it => it.GetIndex() == nextDifficultyIndex);
        return nextDifficulty;
    }
}
