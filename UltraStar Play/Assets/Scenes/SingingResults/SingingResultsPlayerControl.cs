using System;
using System.Globalization;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SingingResultsPlayerControl : INeedInjection, ITranslator, IInjectionFinishedListener, IDisposable
{
    [Inject]
    private SingingResultsSceneControl singingResultsSceneControl;

    [Inject]
    public PlayerProfile PlayerProfile { get; private set; }

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private PlayerScoreControlData playerScoreData;

    [Inject]
    private Statistics statistics;
    
    [Inject]
    private SingingResultsSceneData sceneData;
    
    [Inject(UxmlName = R.UxmlNames.normalNoteScore)]
    private VisualElement normalNoteScoreContainer;

    [Inject(UxmlName = R.UxmlNames.goldenNoteScore)]
    private VisualElement goldenNoteScoreContainer;

    [Inject(UxmlName = R.UxmlNames.phraseBonusScore)]
    private VisualElement phraseBonusScoreContainer;

    [Inject(UxmlName = R.UxmlNames.totalScoreLabel)]
    private Label totalScoreLabel;

    [Inject(UxmlName = R.UxmlNames.playerNameLabel)]
    private Label playerNameLabel;

    [Inject(UxmlName = R.UxmlNames.ratingLabel)]
    private Label ratingLabel;

    [Inject(UxmlName = R.UxmlNames.ratingImage)]
    private VisualElement ratingImage;

    [Inject(UxmlName = R.UxmlNames.playerImage)]
    private VisualElement playerImage;
    public VisualElement PlayerImage => playerImage;

    [Inject(UxmlName = R.UxmlNames.playerScoreProgressBar)]
    private RadialProgressBar playerScoreProgressBar;

    [Inject(UxmlName = R.UxmlNames.newHighscoreContainer)]
    private VisualElement newHighscoreContainer;
    
    [Inject(UxmlName = R.UxmlNames.knockOutLabelOverlay)]
    private VisualElement knockOutLabelOverlay;

    [Inject]
    private SongRating songRating;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    private readonly float animationTimeInSeconds = 1f;

    private int animationId;
    
    public void OnInjectionFinished()
    {
        // Player name and image
        playerNameLabel.text = ShouldShowTeamName()
            ? GetTeamName()
            : PlayerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .CreateAndInject<PlayerProfileImageControl>();

        if (IsNewHighscore())
        {
            newHighscoreContainer.ShowByDisplay();
            // Bouncy size animation
            LeanTween.value(singingResultsSceneControl.gameObject, Vector3.one * 0.75f, Vector3.one, animationTimeInSeconds)
                .setEaseSpring()
                .setOnUpdate(s => newHighscoreContainer.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1))));
        }
        else
        {
            newHighscoreContainer.HideByDisplay();
        }
        
        // Song rating
        LoadSongRatingSprite(songRating.EnumValue, songRatingSprite =>
        {
            if (songRatingSprite == null)
            {
                return;
            }

            ratingImage.style.backgroundImage = new StyleBackground(songRatingSprite);
            AnimationUtils.BounceVisualElementSize(singingResultsSceneControl.gameObject, ratingImage, animationTimeInSeconds);
        });
        ratingLabel.text = songRating.Text;

        // Score texts (animated)
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.NormalNotesTotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(normalNoteScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.GoldenNotesTotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(goldenNoteScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.PerfectSentenceBonusTotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(phraseBonusScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.TotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => totalScoreLabel.text = interpolatedValue.ToStringInvariantCulture("0"));

        // Score bar (animated)
        if (micProfile != null)
        {
            playerScoreProgressBar.ProgressColor = micProfile.Color;
        }

        float playerScoreFactor = (float)playerScoreData.TotalScore / PlayerScoreControl.maxScore;
        animationId = LeanTween.value(singingResultsSceneControl.gameObject, 0, 100f * playerScoreFactor, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => playerScoreProgressBar.ProgressInPercent = interpolatedValue)
            .setEaseOutSine()
            .id;

        knockOutLabelOverlay.HideByDisplay();

        UpdateTranslation();
    }

    private bool IsNewHighscore()
    {
        if (playerScoreData.TotalScore <= 0)
        {
            return false;
        }
        
        LocalStatistic localStatistic = statistics.GetLocalStats(sceneData.SongMetas.LastOrDefault());
        if (localStatistic == null
            || localStatistic.StatsEntries == null
            || localStatistic.StatsEntries.SongStatistics.IsNullOrEmpty())
        {
            return false;
        }
        
        return localStatistic.StatsEntries
            .GetTopScores(1, PlayerProfile.Difficulty)
            .FirstOrDefault().Score == playerScoreData.TotalScore;
    }

    private void LoadSongRatingSprite(ESongRating songRatingEnumValue, Action<Sprite> onSuccess)
    {
        if (settings.DeveloperSettings.disableDynamicThemes
            || themeManager.GetCurrentTheme()?.ThemeJson?.songRatingIcons == null)
        {
            LoadDefaultSongRatingSprite(songRatingEnumValue, onSuccess);
            return;
        }
        LoadSongRatingSpriteFromTheme(songRatingEnumValue, onSuccess);
    }
    
    private string GetTeamName()
    {
        PartyModeTeamSettings teamSettings = PartyModeUtils.GetTeam(singingResultsSceneControl.PartyModeSceneData, PlayerProfile);
        return teamSettings.name;
    }

    private bool ShouldShowTeamName()
    {
        if (singingResultsSceneControl.HasPartyModeSceneData
            && singingResultsSceneControl.PartyModeSettings.teamSettings.isFreeForAll)
        {
            return false;
        }
        PartyModeTeamSettings teamSettings = PartyModeUtils.GetTeam(singingResultsSceneControl.PartyModeSceneData, PlayerProfile);
        return teamSettings != null;
    }

    private void LoadSongRatingSpriteFromTheme(ESongRating songRatingEnumValue, Action<Sprite> onSuccess)
    {
        try
        {
            ThemeMeta themeMeta = themeManager.GetCurrentTheme();
            string valueForSongRating = themeMeta.ThemeJson.songRatingIcons.GetValueForSongRating(songRatingEnumValue);
            if (valueForSongRating.IsNullOrEmpty())
            {
                LoadDefaultSongRatingSprite(songRatingEnumValue, onSuccess);
                return;
            }

            string imagePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, valueForSongRating);
            ImageManager.LoadSpriteFromUri(imagePath, onSuccess);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            LoadDefaultSongRatingSprite(songRatingEnumValue, onSuccess);
        }
    }

    private void LoadDefaultSongRatingSprite(ESongRating songRatingEnumValue, Action<Sprite> onSuccess)
    {
        SongRatingImageReference songRatingImageReference = singingResultsSceneControl.songRatingImageReferences
            .FirstOrDefault(it => it.songRating == songRatingEnumValue);
        onSuccess(songRatingImageReference?.sprite);
    }

    public void UpdateTranslation()
    {
        normalNoteScoreContainer.Q<Label>(R.UxmlNames.scoreName).text = TranslationManager.GetTranslation(R.Messages.score_notes);
        goldenNoteScoreContainer.Q<Label>(R.UxmlNames.scoreName).text = TranslationManager.GetTranslation(R.Messages.score_goldenNotes);
        phraseBonusScoreContainer.Q<Label>(R.UxmlNames.scoreName).text = TranslationManager.GetTranslation(R.Messages.score_phraseBonus);
    }

    private void SetScoreRowLabelText(VisualElement container, float interpolatedValue)
    {
        container.Q<Label>(R.UxmlNames.scoreValue).text = interpolatedValue.ToString("0", CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        LeanTween.cancel(animationId);
    }
    
    public void ShowKnockedOutLabel()
    {
        knockOutLabelOverlay.ShowByDisplay();
    }
}
