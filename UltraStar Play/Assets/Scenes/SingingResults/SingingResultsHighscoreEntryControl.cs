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

    [Inject(UxmlName = R.UxmlNames.highScoreSourceIcon)]
    private VisualElement highScoreSourceIcon;

    [Inject(Key = "entryIndex")]
    private int index;

    [Inject]
    private HighScoreEntry highScoreEntry;

    public void OnInjectionFinished()
    {
        visualElement.AddToClassList($"highscoreEntry-{index}");
        posLabel.SetTranslatedText(Translation.Of((index + 1).ToString()));
        playerNameLabel.SetTranslatedText(Translation.Of(highScoreEntry.PlayerName));
        scoreLabel.SetTranslatedText(Translation.Of(highScoreEntry.Score.ToString()));
        dateLabel.SetTranslatedText(Translation.Of(highScoreEntry.DateTime.ToString("d", CultureInfo.CurrentUICulture)));
        commonScoreIcon.HideByDisplay();

        highScoreSourceIcon.SetVisibleByDisplay(!highScoreEntry.RemoteSource.IsNullOrEmpty());
        if (!highScoreEntry.RemoteSource.IsNullOrEmpty())
        {
            new TooltipControl(highScoreSourceIcon, Translation.Of(highScoreEntry.RemoteSource));
        }
    }
}
