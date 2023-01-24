using System;
using System.Globalization;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SingingResultsPlayerControl : INeedInjection, ITranslator, IInjectionFinishedListener
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

    [Inject(UxmlName = R.UxmlNames.totalScore)]
    private VisualElement totalScoreContainer;

    [Inject(UxmlName = R.UxmlNames.playerNameLabel)]
    private Label playerNameLabel;

    [Inject(UxmlName = R.UxmlNames.ratingLabel)]
    private Label ratingLabel;

    [Inject(UxmlName = R.UxmlNames.ratingImage)]
    private VisualElement ratingImage;

    [Inject(UxmlName = R.UxmlNames.playerImage)]
    private VisualElement playerImage;

    [Inject(UxmlName = R.UxmlNames.filledScoreBar)]
    private VisualElement filledScoreBar;

    [Inject]
    private SongRating songRating;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    private readonly float animationTimeInSeconds = 1f;

    public void OnInjectionFinished()
    {
        // Player name and image
        playerNameLabel.text = playerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .CreateAndInject<AvatarImageControl>();

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
            .setOnUpdate(interpolatedValue => SetScoreLabelText(normalNoteScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.GoldenNotesTotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreLabelText(goldenNoteScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.PerfectSentenceBonusTotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreLabelText(phraseBonusScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, playerScoreData.TotalScore, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreLabelText(totalScoreContainer, interpolatedValue));

        // Score bar (animated)
        float playerScorePercent = (float)playerScoreData.TotalScore / PlayerScoreControl.maxScore;
        float minScoreBarHeightInPercent = 5f;
        float maxScoreBarHeightInPercent = minScoreBarHeightInPercent + ((100f - minScoreBarHeightInPercent) * playerScorePercent);
        LeanTween.value(singingResultsSceneControl.gameObject, minScoreBarHeightInPercent, maxScoreBarHeightInPercent, animationTimeInSeconds)
            .setOnUpdate(interpolatedValue => filledScoreBar.style.height = new StyleLength(new Length(interpolatedValue, LengthUnit.Percent)))
            .setEaseOutSine();

        UpdateTranslation();
    }

    private void LoadSongRatingSprite(SongRating.ESongRating songRatingEnumValue, Action<Sprite> onSuccess)
    {
        if (settings.DeveloperSettings.disableDynamicThemes
            || themeManager.GetCurrentTheme()?.ThemeJson?.songRatingIcons == null)
        {
            LoadDefaultSongRatingSprite(songRatingEnumValue, onSuccess);
            return;
        }
        LoadSongRatingSpriteFromTheme(songRatingEnumValue, onSuccess);
    }

    private void LoadSongRatingSpriteFromTheme(SongRating.ESongRating songRatingEnumValue, Action<Sprite> onSuccess)
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

    private void LoadDefaultSongRatingSprite(SongRating.ESongRating songRatingEnumValue, Action<Sprite> onSuccess)
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
        totalScoreContainer.Q<Label>(R.UxmlNames.scoreName).text = TranslationManager.GetTranslation(R.Messages.score_total);
    }

    private void SetScoreLabelText(VisualElement container, float interpolatedValue)
    {
        container.Q<Label>(R.UxmlNames.scoreValue).text = interpolatedValue.ToString("0", CultureInfo.InvariantCulture);
    }
}
