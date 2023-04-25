using System.Collections.Generic;
using System.Linq;
using UniInject;
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
            : settings.GameSettings.Difficulty;
        nextDifficultyButton.RegisterCallbackButtonTriggered(_ => ChangeDifficulty(1));
        previousDifficultyButton.RegisterCallbackButtonTriggered(_ => ChangeDifficulty(-1));
        UpdateHighscores();
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
        UpdateHighscores();
    }

    private void UpdateHighscores()
    {
        currentDifficultyLabel.text = currentDifficulty.GetTranslatedName();

        highscoreEntryList.Clear();
        LocalStatistic localStatistic = statistics.GetLocalStats(sceneData.SongMetas.LastOrDefault());
        List<SongStatistic> songStatistics = localStatistic?.StatsEntries?.SongStatistics?
            .Where(it => it.Difficulty == currentDifficulty).ToList();
        
        if (songStatistics.IsNullOrEmpty())
        {
            Label noHighscoresLabel = new Label("No highscores yet");
            noHighscoresLabel.name = "noHighscoresLabel";
            highscoreEntryList.Add(noHighscoresLabel);
            return;
        }
        
        songStatistics.Sort(new CompareBySongScoreDescending());
        List<SongStatistic> topSongStatistics = songStatistics.Take(highscoreCount).ToList();
        for (int i = 0; i < topSongStatistics.Count; i++)
        {
            CreateHighscoreEntry(topSongStatistics[i], i);
        }
    }

    private void CreateHighscoreEntry(SongStatistic songStatistic, int index)
    {
        VisualElement highscoreEntry = highscoreEntryUi.CloneTree().Children().FirstOrDefault();
        highscoreEntryList.Add(highscoreEntry);

        injector
            .WithRootVisualElement(highscoreEntry)
            .WithBindingForInstance(songStatistic)
            .WithBinding(new Binding("entryIndex", new ExistingInstanceProvider<int>(index)))
            .CreateAndInject<SingingResultsHighscoreEntryControl>();
    }
}
