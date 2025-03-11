using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SingingResultsPlayerControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject]
    private SingingResultsSceneControl singingResultsSceneControl;

    [Inject]
    public PlayerProfile PlayerProfile { get; private set; }

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private ISingingResultsPlayerScore singingResultsPlayerScore;

    [Inject]
    private Statistics statistics;

    [Inject]
    private SingingResultsSceneData sceneData;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R.UxmlNames.normalNoteScore)]
    private VisualElement normalNoteScoreContainer;

    [Inject(UxmlName = R.UxmlNames.goldenNoteScore)]
    private VisualElement goldenNoteScoreContainer;

    [Inject(UxmlName = R.UxmlNames.phraseBonusScore)]
    private VisualElement phraseBonusScoreContainer;

    [Inject(UxmlName = R.UxmlNames.modBonusScore)]
    private VisualElement modBonusScoreContainer;

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

    [Inject(UxmlName = R.UxmlNames.newHighscoreContainer)]
    private VisualElement newHighscoreContainer;

    [Inject(UxmlName = R.UxmlNames.songRatingStarIcon)]
    private List<VisualElement> songRatingStarIcons;

    [Inject]
    private SongRating songRating;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    private readonly float bounceAnimTimeInSeconds = 1f;
    private readonly float maxScoreAnimationTimeInSeconds = 5f;
    private float NormalNoteAnimTimeInSeconds => maxScoreAnimationTimeInSeconds * ((float)singingResultsPlayerScore.NormalNotesTotalScore / PlayerScoreControl.maxScore);
    private float GoldenNoteAnimTimeInSeconds => maxScoreAnimationTimeInSeconds * ((float)singingResultsPlayerScore.GoldenNotesTotalScore / PlayerScoreControl.maxScore);
    private float PerfectSentenceBonusAnimTimeInSeconds => maxScoreAnimationTimeInSeconds * ((float)singingResultsPlayerScore.PerfectSentenceBonusTotalScore / PlayerScoreControl.maxScore);
    private float ModBonusAnimTimeInSeconds => maxScoreAnimationTimeInSeconds * ((float)Math.Abs(singingResultsPlayerScore.ModTotalScore) / PlayerScoreControl.maxScore);
    private float TotalScoreAnimTimeInSeconds => NormalNoteAnimTimeInSeconds + GoldenNoteAnimTimeInSeconds + PerfectSentenceBonusAnimTimeInSeconds + ModBonusAnimTimeInSeconds;

    private readonly List<int> animationIds = new();

    public void OnInjectionFinished()
    {
        // Player name and image
        playerNameLabel.SetTranslatedText(Translation.Of(ShouldShowTeamName()
            ? GetTeamName()
            : PlayerProfile.Name));
        injector.WithRootVisualElement(playerImage)
            .CreateAndInject<PlayerProfileImageControl>();

        newHighscoreContainer.HideByVisibility();
        if (IsNewHighscore())
        {
            // Bouncy size animation
            LeanTween.value(singingResultsSceneControl.gameObject, Vector3.one * 0.75f, Vector3.one, bounceAnimTimeInSeconds)
                .setEaseSpring()
                .setOnStart(() => newHighscoreContainer.ShowByVisibility())
                .setOnUpdate(s => newHighscoreContainer.style.scale = new StyleScale(new Scale(new Vector2(s, s))))
                .setDelay(TotalScoreAnimTimeInSeconds);
        }

        // Song rating
        InitSongRatingSpriteAsync();
        ratingLabel.SetTranslatedText(songRating.Translation);

        // Score texts (animated)
        ResetScoreRowLabelTexts();
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, singingResultsPlayerScore.NormalNotesTotalScore, NormalNoteAnimTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(normalNoteScoreContainer, interpolatedValue));
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, singingResultsPlayerScore.GoldenNotesTotalScore, GoldenNoteAnimTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(goldenNoteScoreContainer, interpolatedValue))
            .setDelay(NormalNoteAnimTimeInSeconds);
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, singingResultsPlayerScore.PerfectSentenceBonusTotalScore, PerfectSentenceBonusAnimTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(phraseBonusScoreContainer, interpolatedValue))
            .setDelay(NormalNoteAnimTimeInSeconds + GoldenNoteAnimTimeInSeconds);
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, singingResultsPlayerScore.ModTotalScore, ModBonusAnimTimeInSeconds)
            .setOnUpdate(interpolatedValue => SetScoreRowLabelText(modBonusScoreContainer, interpolatedValue))
            .setDelay(NormalNoteAnimTimeInSeconds + GoldenNoteAnimTimeInSeconds + PerfectSentenceBonusAnimTimeInSeconds);
        LeanTween.value(singingResultsSceneControl.gameObject, 0f, singingResultsPlayerScore.TotalScore, TotalScoreAnimTimeInSeconds)
            .setOnUpdate(interpolatedValue => totalScoreLabel.SetTranslatedText(Translation.Of(interpolatedValue.ToStringInvariantCulture("0"))));

        // Score bar (animated)
        Color32 scoreBarColor = CommonOnlineMultiplayerUtils.GetPlayerColor(PlayerProfile, micProfile);
        playerScoreProgressBar.ProgressColor = scoreBarColor;
        if (scoreBarColor == Color.clear)
        {
            // Do not show border because it looks bad without a fill color
            playerImage.SetBorderWidth(0);
        }

        float playerScoreFactor = (float)singingResultsPlayerScore.TotalScore / PlayerScoreControl.maxScore;
        animationIds.Add(LeanTween.value(singingResultsSceneControl.gameObject, 0, 100f * playerScoreFactor, TotalScoreAnimTimeInSeconds)
            .setOnUpdate(interpolatedValue => playerScoreProgressBar.ProgressInPercent = interpolatedValue)
            .setEaseOutSine()
            .id);

        // Stars (animated)
        // AnimateStarRatingIcons();

        UpdateTranslation();
    }

    private async void InitSongRatingSprite()
    {
        await InitSongRatingSpriteAsync();
    }

    private async Awaitable InitSongRatingSpriteAsync()
    {
        Sprite songRatingSprite = await LoadSongRatingSpriteAsync(songRating.EnumValue);
        if (songRatingSprite == null)
        {
            return;
        }

        ratingImage.style.backgroundImage = new StyleBackground(songRatingSprite);
        // Bouncy size animation
        ratingLabel.style.scale = new StyleScale(new Scale(Vector2.zero));
        ratingImage.style.scale = new StyleScale(new Scale(Vector2.zero));
        LeanTween.value(singingResultsSceneControl.gameObject, Vector2.one, Vector2.one * 0.5f, bounceAnimTimeInSeconds)
            .setEasePunch()
            .setOnStart(() =>
            {
                if (TotalScoreAnimTimeInSeconds > 0)
                {
                    PlaySingingResultsRatingPopupSound();
                }
            })
            .setOnUpdate(s =>
            {
                Vector2 scale = new Vector2(s, s);
                ratingLabel.style.scale = new StyleScale(new Scale(scale));
                ratingImage.style.scale = new StyleScale(new Scale(scale));
            })
            .setDelay(TotalScoreAnimTimeInSeconds);

    }

    private void PlaySingingResultsRatingPopupSound()
    {
        SfxManager.PlaySingingResultsRatingPopupSound();
    }

    private void ResetScoreRowLabelTexts()
    {
        SetScoreRowLabelText(normalNoteScoreContainer, 0);
        SetScoreRowLabelText(goldenNoteScoreContainer, 0);
        SetScoreRowLabelText(phraseBonusScoreContainer, 0);
        SetScoreRowLabelText(modBonusScoreContainer, 0);
    }

    private void AnimateStarRatingIcons()
    {
        songRatingStarIcons.ForEach(it => it.style.scale = Vector2.zero);
        int starCount = SongSelectSongRatingIconControl.GetStarCount(singingResultsPlayerScore.TotalScore);

        // Skip the center star if even number of stars visible
        List<VisualElement> visibleStarIcons = starCount % 2 == 1
            ? songRatingStarIcons.Take(starCount).ToList()
            : songRatingStarIcons.Skip(1).Take(starCount).ToList();

        float starIconAnimationTimeInSeconds = TotalScoreAnimTimeInSeconds;
        for (int i = 0; i < visibleStarIcons.Count; i++)
        {
            VisualElement visibleStarIcon = visibleStarIcons[i];
            animationIds.Add(LeanTween.value(singingResultsSceneControl.gameObject, 0, 1, starIconAnimationTimeInSeconds)
                .setDelay(i * starIconAnimationTimeInSeconds / 2)
                .setOnUpdate(interpolatedValue => visibleStarIcon.style.scale = new Vector2(interpolatedValue, interpolatedValue))
                .setEasePunch()
                .id);
        }
    }

    private bool IsNewHighscore()
    {
        if (singingResultsPlayerScore.TotalScore <= 0
            || sceneData.GameRoundSettings == null
            || sceneData.GameRoundSettings.AnyModifierActive)
        {
            return false;
        }

        SongMeta songMeta = sceneData.SongMetas.LastOrDefault();
        SongStatistics songStatistics = StatisticsUtils.GetLocalSongStatistics(statistics, songMeta);
        if (songStatistics == null
            || songStatistics.HighScoreRecord == null
            || songStatistics.HighScoreRecord.HighScoreEntries.IsNullOrEmpty())
        {
            return false;
        }

        HighScoreEntry highScoreEntry = StatisticsUtils.GetTopScores(
                songStatistics.HighScoreRecord.HighScoreEntries,
                1,
                PlayerProfile.Difficulty)
            .FirstOrDefault();
        if (highScoreEntry == null)
        {
            return false;
        }

        return highScoreEntry.Score == singingResultsPlayerScore.TotalScore;
    }

    private async Awaitable<Sprite> LoadSongRatingSpriteAsync(ESongRating songRatingEnumValue)
    {
        if (!settings.EnableDynamicThemes
            || themeManager.GetCurrentTheme()?.ThemeJson?.songRatingIcons == null)
        {
            return await LoadDefaultSongRatingSpriteAsync(songRatingEnumValue);
        }
        return await LoadSongRatingSpriteFromThemeAsync(songRatingEnumValue);
    }

    private string GetTeamName()
    {
        PartyModeTeamSettings teamSettings = PartyModeUtils.GetTeam(singingResultsSceneControl.PartyModeSceneData, PlayerProfile);
        return teamSettings.name;
    }

    private bool ShouldShowTeamName()
    {
        if (singingResultsSceneControl.HasPartyModeSceneData
            && singingResultsSceneControl.PartyModeSettings.TeamSettings.IsFreeForAll)
        {
            return false;
        }
        PartyModeTeamSettings teamSettings = PartyModeUtils.GetTeam(singingResultsSceneControl.PartyModeSceneData, PlayerProfile);
        return teamSettings != null;
    }

    private async Awaitable<Sprite> LoadSongRatingSpriteFromThemeAsync(ESongRating songRatingEnumValue)
    {
        try
        {
            ThemeMeta themeMeta = themeManager.GetCurrentTheme();
            string valueForSongRating = themeMeta.ThemeJson.songRatingIcons.GetValueForSongRating(songRatingEnumValue);
            if (valueForSongRating.IsNullOrEmpty())
            {
                return await LoadDefaultSongRatingSpriteAsync(songRatingEnumValue);
            }

            string imagePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, valueForSongRating);
            return await ImageManager.LoadSpriteFromUriAsync(imagePath);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Load song rating sprite from theme failed: {ex.Message}");
            return await LoadDefaultSongRatingSpriteAsync(songRatingEnumValue);
        }
    }

    private async Awaitable<Sprite> LoadDefaultSongRatingSpriteAsync(ESongRating songRatingEnumValue)
    {
        SongRatingImageReference songRatingImageReference = singingResultsSceneControl.songRatingImageReferences
            .FirstOrDefault(it => it.songRating == songRatingEnumValue);
        return songRatingImageReference?.sprite;
    }

    public void UpdateTranslation()
    {
        normalNoteScoreContainer.Q<Label>(R.UxmlNames.scoreName).SetTranslatedText(Translation.Get(R.Messages.score_notes));
        goldenNoteScoreContainer.Q<Label>(R.UxmlNames.scoreName).SetTranslatedText(Translation.Get(R.Messages.score_goldenNotes));
        phraseBonusScoreContainer.Q<Label>(R.UxmlNames.scoreName).SetTranslatedText(Translation.Get(R.Messages.score_perfectSentenceBonus));
    }

    private void SetScoreRowLabelText(VisualElement container, float interpolatedValue)
    {
        container.Q<Label>(R.UxmlNames.scoreValue).SetTranslatedText(
            Translation.Of(interpolatedValue.ToString("0", CultureInfo.InvariantCulture)));
    }

    public void Dispose()
    {
        LeanTweenUtils.CancelAndClear(animationIds);
    }

    public void InitTopScoreVfx()
    {
        AwaitableUtils.ExecuteAfterDelayInSecondsAsync(singingResultsSceneControl.gameObject, TotalScoreAnimTimeInSeconds, () =>
        {
            VfxManager.CreateParticleEffect(new ParticleEffectConfig()
            {
                particleEffect = EParticleEffect.LightGlowALoop,
                panelPos = playerImage.worldBound.center,
                scale = 0.4f,
                loop = true,
                isBackground = true,
                target = playerImage,
                hideAndShowWithTarget = true,
            });
        });
    }

    public void SetModScoreVisible(bool isVisible)
    {
        modBonusScoreContainer.SetVisibleByDisplay(isVisible);
    }
}
