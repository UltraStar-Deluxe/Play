using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsHighscoreControl : INeedInjection
{
    [Inject(Key = nameof(highscoreEntryUi))]
    private VisualTreeAsset highscoreEntryUi;

    [Inject(UxmlName = R.UxmlNames.previousDifficultyButton)]
    private Button previousDifficultyButton;

    [Inject(UxmlName = R.UxmlNames.currentDifficultyLabel)]
    private Label currentDifficultyLabel;

    [Inject(UxmlName = R.UxmlNames.nextDifficultyButton)]
    private Button nextDifficultyButton;

    [Inject(UxmlName = R.UxmlNames.highscoreEntryList)]
    private VisualElement highscoreEntryList;

    [Inject]
    private Statistics statistics;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SingingResultsSceneData sceneData;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly int highscoreCount = 5;

    private bool isInitialized;

    private EDifficulty currentDifficulty;

    public void Init()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        currentDifficulty = !sceneData.PlayerProfiles.IsNullOrEmpty()
            ? sceneData.PlayerProfiles.FirstOrDefault().Difficulty
            : settings.Difficulty;
        nextDifficultyButton.RegisterCallbackButtonTriggered(_ => ChangeDifficulty(1));
        previousDifficultyButton.RegisterCallbackButtonTriggered(_ => ChangeDifficulty(-1));
        UpdateHighScores();
    }

    private void ChangeDifficulty(int direction)
    {
        List<EDifficulty> difficulties = EnumUtils.GetValuesAsList<EDifficulty>();
        if (direction < 0)
        {
            currentDifficulty = difficulties.GetElementBefore(currentDifficulty, true);
        }
        else if (direction > 0)
        {
            currentDifficulty = difficulties.GetElementAfter(currentDifficulty, true);
        }
        UpdateHighScores();
    }

    private void UpdateHighScores()
    {
        currentDifficultyLabel.SetTranslatedText(Translation.Get(currentDifficulty));

        highscoreEntryList.Clear();

        SongMeta songMeta = sceneData.SongMetas.LastOrDefault();
        StatisticsUtils.GetLocalAndRemoteHighScoreEntriesAllAtOnce(statistics, songMeta)
            .Subscribe(scoreEntries =>
            {
                List<HighScoreEntry> scoreEntriesOfCurrentDifficulty = scoreEntries
                    .Where(entry => entry.Difficulty == currentDifficulty)
                    .ToList();
                UpdateHighScores(scoreEntriesOfCurrentDifficulty);
            });
    }

    private void UpdateHighScores(List<HighScoreEntry> highScoreEntries)
    {
        if (highScoreEntries.IsNullOrEmpty())
        {
            Label noHighscoresLabel = new Label("No high scores yet");
            noHighscoresLabel.name = "noHighscoresLabel";
            highscoreEntryList.Add(noHighscoresLabel);
            return;
        }

        highScoreEntries.Sort(new HighScoreEntry.CompareByScoreDescending());
        List<HighScoreEntry> topSongEntries = highScoreEntries.Take(highscoreCount).ToList();
        for (int i = 0; i < topSongEntries.Count; i++)
        {
            CreateHighscoreEntry(topSongEntries[i], i);
        }
    }

    private void CreateHighscoreEntry(HighScoreEntry highScoreEntry, int index)
    {
        VisualElement highscoreEntry = highscoreEntryUi.CloneTree().Children().FirstOrDefault();
        highscoreEntryList.Add(highscoreEntry);

        injector
            .WithRootVisualElement(highscoreEntry)
            .WithBindingForInstance(highScoreEntry)
            .WithBinding(new UniInjectBinding("entryIndex", new ExistingInstanceProvider<int>(index)))
            .CreateAndInject<SingingResultsHighscoreEntryControl>();
    }
}
