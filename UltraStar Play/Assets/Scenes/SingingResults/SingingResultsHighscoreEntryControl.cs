using System.Globalization;
using UniInject;
using UnityEngine.UIElements;

public class SingingResultsHighscoreEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;
    
    [Inject(UxmlName = R.UxmlNames.posLabel)]
    private Label posLabel;
    
    [Inject(UxmlName = R.UxmlNames.playerNameLabel)]
    private Label playerNameLabel;
    
    [Inject(UxmlName = R.UxmlNames.scoreLabel)]
    private Label scoreLabel;
    
    [Inject(UxmlName = R.UxmlNames.dateLabel)]
    private Label dateLabel;
    
    [Inject(UxmlName = R.UxmlNames.commonScoreIcon)]
    private VisualElement commonScoreIcon;
    
    [Inject(Key = "entryIndex")]
    private int index;
    
    [Inject]
    private SongStatistic songStatistic;
    
    public void OnInjectionFinished()
    {
        visualElement.AddToClassList($"highscoreEntry-{index}");
        posLabel.text = (index + 1).ToString();
        playerNameLabel.text = songStatistic.PlayerName;
        scoreLabel.text = songStatistic.Score.ToString();
        dateLabel.text = songStatistic.DateTime.ToString("d", CultureInfo.CurrentUICulture);
        commonScoreIcon.SetVisibleByDisplay(songStatistic.ScoreMode == EScoreMode.CommonAverage);
    }
}
