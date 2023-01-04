using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HighscoreSceneControl : MonoBehaviour, INeedInjection, IBinder, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.hiddenContinueButton)]
    private Button hiddenContinueButton;

    [Inject(UxmlName = R.UxmlNames.nextDifficultyButton)]
    private Button nextDifficultyButton;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.sceneSubtitle)]
    private Label titleAndArtistText;

    [Inject(UxmlName = R.UxmlNames.difficultyLabel)]
    private Label difficultyText;

    [Inject(UxmlName = R.UxmlNames.highscoreEntry)]
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
        continueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        hiddenContinueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        nextDifficultyButton.RegisterCallbackButtonTriggered(() => ShowNextDifficulty(1));
        ShowHighscores(sceneData.SongMeta, sceneData.Difficulty);

        // Click through to hiddenContinueButton
        uiDocument.rootVisualElement.Query<VisualElement>()
            .ToList()
            .ForEach(visualElement => visualElement.pickingMode = visualElement is Button
                ? PickingMode.Position
                : PickingMode.Ignore);

        continueButton.Focus();
    }

    public void FinishScene()
    {
        SongSelectSceneData songSelectSceneData = new();
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
                highscoreEntries[i].ShowByDisplay();
                FillHighscoreEntry(highscoreEntries[i], topSongStatistics[i], i);
            }
            else
            {
                highscoreEntries[i].HideByDisplay();
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
        highscoreEntry.Q<VisualElement>(R.UxmlNames.commonScoreIcon).SetVisibleByDisplay(songStatistic.ScoreMode == EScoreMode.CommonAverage);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.highscoreScene_title);
        continueButton.text = TranslationManager.GetTranslation(R.Messages.continue_);

        nextDifficultyButton.Q<Label>().text = currentDifficulty.GetTranslatedName();
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
