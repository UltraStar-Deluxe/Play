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

public class HighscoreSceneControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IBinder, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

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
    private SongQueueManager songQueueManager;

    [Inject]
    private Statistics statistics;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private HighscoreSceneData sceneData;

    private EDifficulty currentDifficulty;

    private NextGameRoundUiControl nextGameRoundUiControl = new();

    public void OnInjectionFinished()
    {
        injector.Inject(nextGameRoundUiControl);
    }

    private void Start()
    {
        continueButton.RegisterCallbackButtonTriggered(_ => FinishScene());
        nextDifficultyButton.RegisterCallbackButtonTriggered(_ => ShowNextDifficulty(1));
        ShowHighscores(sceneData.SongMeta, sceneData.Difficulty);

        continueButton.Focus();
    }

    public void FinishScene()
    {
        if (songQueueManager.IsSongQueueEmpty)
        {
            // Go to song select
            SongSelectSceneData songSelectSceneData = new();
            songSelectSceneData.SongMeta = sceneData.SongMeta;
            sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
        }
        else
        {
            // Start next game round
            SingSceneData singSceneData = songQueueManager.CreateNextSingSceneData(sceneData.partyModeSceneData);
            sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
        }
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

        SongStatistics songStatistics = statistics.GetLocalStatistics(songMeta);
        List<HighScoreEntry> highScoreEntries = songStatistics?.HighScoreRecord?.HighScoreEntries?
            .Where(it => it.Difficulty == difficulty).ToList();
        if (highScoreEntries.IsNullOrEmpty())
        {
            highScoreEntries = new List<HighScoreEntry>();
        }
        highScoreEntries.Sort(new CompareBySongScoreDescending());
        List<HighScoreEntry> topScoreEntries = highScoreEntries.Take(highscoreEntries.Count).ToList();
        for (int i = 0; i < highscoreEntries.Count; i++)
        {
            if (i < topScoreEntries.Count)
            {
                highscoreEntries[i].ShowByDisplay();
                FillHighscoreEntry(highscoreEntries[i], topScoreEntries[i], i);
            }
            else
            {
                highscoreEntries[i].HideByDisplay();
            }
        }

        // update "next difficulty button" text
        UpdateTranslation();
    }

    private void FillHighscoreEntry(VisualElement highscoreEntry, HighScoreEntry highScoreEntry, int index)
    {
        highscoreEntry.Q<Label>(R.UxmlNames.posLabel).text = (index + 1).ToString();
        highscoreEntry.Q<Label>(R.UxmlNames.playerNameLabel).text = highScoreEntry.PlayerName;
        highscoreEntry.Q<Label>(R.UxmlNames.scoreLabel).text = highScoreEntry.Score.ToString();
        highscoreEntry.Q<Label>(R.UxmlNames.dateLabel).text = highScoreEntry.DateTime.ToString("d", CultureInfo.CurrentUICulture);
        // highscoreEntry.Q<VisualElement>(R.UxmlNames.commonScoreIcon).SetVisibleByDisplay(songStatistic.ScoreMode == EScoreMode.CommonAverage);
        highscoreEntry.Q<VisualElement>(R.UxmlNames.commonScoreIcon).HideByDisplay();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(SceneNavigator.GetSceneDataOrThrow<HighscoreSceneData>());
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
        int nextDifficultyIndex = NumberUtils.ModNegativeToPositive(currentDifficultyIndex + direction, EnumUtils.GetValuesAsList<EDifficulty>().Count);
        EDifficulty nextDifficulty = EnumUtils.GetValuesAsList<EDifficulty>()
            .FirstOrDefault(it => it.GetIndex() == nextDifficultyIndex);
        return nextDifficulty;
    }
}
