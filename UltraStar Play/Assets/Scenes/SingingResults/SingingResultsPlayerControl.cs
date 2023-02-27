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
    private PlayerProfile playerProfile;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private PlayerScoreControlData playerScoreData;

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

    [Inject(UxmlName = R.UxmlNames.playerScoreProgressBar)]
    private RadialProgressBar playerScoreProgressBar;

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
        playerNameLabel.text = playerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .CreateAndInject<PlayerProfileImageControl>();

        // Song rating
        LoadSongRatingSprite(songRating.EnumValue, songRatingSprite =>
        {
            if (songRatingSprite == null)
            {
                return;
            }

            ratingImage.style.backgroundImage = new StyleBackground(songRatingSprite);
                // Bouncy size animation
                LeanTween.value(singingResultsSceneControl.gameObject, Vector3.one * 0.75f, Vector3.one, animationTimeInSeconds)
                    .setEaseSpring()
                    .setOnUpdate(s => ratingImage.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1))));
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
            playerScoreProgressBar.progressColor = micProfile.Color;
        }

        float playerScoreFactor = (float)playerScoreData.TotalScore / PlayerScoreControl.maxScore;
        animationId = LeanTween.value(singingResultsSceneControl.gameObject, 0, 100f * playerScoreFactor, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => playerScoreProgressBar.progress = interpolatedValue)
            .setEaseOutSine()
            .id;

        UpdateTranslation();
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
}
