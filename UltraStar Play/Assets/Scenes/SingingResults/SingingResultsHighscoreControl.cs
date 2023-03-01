using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsHighscoreControl : INeedInjection
{
    [Inject(Key = nameof(highscoreEntryUi))]
    private VisualTreeAsset highscoreEntryUi;
    
    [Inject(UxmlName = R.UxmlNames.difficultyPicker)]
    private ItemPicker difficultyPicker;
    
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
    
    private LabeledItemPickerControl<EDifficulty> difficultyPickerControl;

    private readonly int highscoreCount = 5;
    
    private bool isInitialized;
    
    public void Init()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;
        
        difficultyPickerControl = new LabeledItemPickerControl<EDifficulty>(difficultyPicker, EnumUtils.GetValuesAsList<EDifficulty>());
        difficultyPickerControl.GetLabelTextFunction = item => item.GetTranslatedName();
        difficultyPickerControl.Selection.Subscribe(item => ShowHighscores(item));
        difficultyPickerControl.SelectItem(sceneData.PlayerProfiles.FirstOrDefault().Difficulty);
    }
    
    private void ShowHighscores(EDifficulty difficulty)
    {
        highscoreEntryList.Clear();
        LocalStatistic localStatistic = statistics.GetLocalStats(sceneData.SongMeta);
        List<SongStatistic> songStatistics = localStatistic?.StatsEntries?.SongStatistics?
            .Where(it => it.Difficulty == difficulty).ToList();
        
        // Test data
        // songStatistics.Add(new SongStatistic("Some player & Other player", EDifficulty.Medium, 500, EScoreMode.CommonAverage));
        
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

        SingingResultsHighscoreEntryControl entryControl = injector
            .WithRootVisualElement(highscoreEntry)
            .WithBindingForInstance(songStatistic)
            .WithBinding(new Binding("entryIndex", new ExistingInstanceProvider<int>(index)))
            .CreateAndInject<SingingResultsHighscoreEntryControl>();
    }
}
