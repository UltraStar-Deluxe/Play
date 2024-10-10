using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private PlayerScoreControl playerScoreControl;

    [Inject]
    private PlayerPerformanceAssessmentControl playerPerformanceAssessmentControl;

    [Inject]
    private ThemeManager themeManager;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement RootVisualElement { get; private set; }

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject(Key = nameof(sentenceRatingUi))]
    private VisualTreeAsset sentenceRatingUi;

    [Inject(UxmlName = R.UxmlNames.sentenceRatingContainer)]
    private VisualElement sentenceRatingContainer;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject(UxmlName = R.UxmlNames.playerScoreLabel)]
    private Label playerScoreLabel;

    [Inject(UxmlName = R.UxmlNames.micDisconnectedIcon)]
    private VisualElement micDisconnectedIcon;

    [Inject(UxmlName = R.UxmlNames.playerImage)]
    private VisualElement playerImage;

    [Inject(UxmlName = R.UxmlNames.playerImageContainer)]
    private VisualElement playerImageContainer;

    [Inject(UxmlName = R.UxmlNames.playerImageBorder)]
    private VisualElement playerImageBorder;

    [Inject(UxmlName = R.UxmlNames.playerNameLabel)]
    private Label playerNameLabel;

    [Inject(UxmlName = R.UxmlNames.leadingPlayerIcon)]
    private VisualElement leadingPlayerIcon;

    [Inject(UxmlName = R.UxmlNames.playerScoreProgressBar)]
    private RadialProgressBar playerScoreProgressBar;

    [Inject(UxmlName = R.UxmlNames.nextPlayerNameLabel)]
    private Label nextPlayerNameLabel;

    [Inject(UxmlName = R.UxmlNames.noteContainer)]
    private VisualElement noteContainer;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;
    public Injector Injector => injector;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneData sceneData;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private AchievementEventStream achievementEventStream;

    [Inject]
    private Voice voice;

    private Color32 PlayerColor => CommonOnlineMultiplayerUtils.GetPlayerColor(playerProfile, micProfile);

    private AbstractSingSceneNoteDisplayer noteDisplayer;
    public AbstractSingSceneNoteDisplayer NoteDisplayer => noteDisplayer;

    private PlayerProfile nextPlayerProfile;
    private float displayNextPlayerProfileTimeInSeconds;

    private int totalScoreAnimationId;
    private int micDisconnectedAnimationId;
    private int leadingPlayerIconAnimationId;
    private int fadeOutNotesAnimationId;

    private Dictionary<ESentenceRating, Color32> sentenceRatingColors;

    private readonly PlayerProfileImageControl playerProfileImageControl = new();
    private readonly PlayerPitchIndicatorControl playerPitchIndicatorControl = new();

    private float setNextPlayerProfileAnimTimeInSeconds = 1.5f;
    private int lastDisplayedScore;

    public void OnInjectionFinished()
    {
        InitPlayerNameAndImage();
        InitNoteDisplayer();

        injector
            .WithBindingForInstance(noteDisplayer)
            .Inject(playerPitchIndicatorControl);

        // Show rating and score after each sentence
        playerScoreLabel.SetTranslatedText(Translation.Empty);

        if (singSceneControl.IsIndividualScore)
        {
            ShowTotalScore(playerScoreControl.TotalScore);
            playerPerformanceAssessmentControl.SentenceAssessedEventStream.Subscribe(evt =>
            {
                ShowSentenceRating(evt.SentenceRating, sentenceRatingContainer);
            });
            playerScoreControl.ScoreChangedEventStream.Subscribe(evt =>
            {
                ShowTotalScore(evt.TotalScore);
            });
        }
        else if (settings.ScoreMode is EScoreMode.None)
        {
            playerScoreProgressBar.HideByDisplay();
        }

        // Show an effect for perfectly sung notes
        playerPerformanceAssessmentControl.NoteAssessedEventStream.Subscribe(evt =>
        {
            if (evt.IsPerfect)
            {
                CreatePerfectNoteEffect(evt.Note);
            }
        });

        // Show message when mic (dis)connects.
        HideMicDisconnectedInfo();
        if (micProfile != null
            && micProfile.IsInputFromConnectedClient)
        {
            serverSideCompanionClientManager.ClientConnectionChangedEventStream
                .Subscribe(OnClientConnectionChanged)
                .AddTo(singSceneControl);
        }

        nextPlayerNameLabel.HideByDisplay();

        // Single perfect sentence effect
        playerPerformanceAssessmentControl.SentenceAssessedEventStream
            .Where(evt => evt.IsPerfect)
            .Subscribe(xs => CreateSinglePerfectSentenceEffect());

        // Create effect when there are at least two perfect sentences in a row.
        playerPerformanceAssessmentControl.SentenceAssessedEventStream.Buffer(2, 1)
            .Where(events => events.AllMatch(evt => evt.IsPerfect))
            .Subscribe(evt => CreateMultiplePerfectSentenceEffect());

        ChangeLayoutByPlayerCount();

        sentenceRatingColors = themeManager.GetSentenceRatingColors();
    }

    private void InitPlayerNameAndImage()
    {
        HideLeadingPlayerIcon();

        if (settings.ScoreMode == EScoreMode.None
            && (settings.NoteDisplayMode == ENoteDisplayMode.None
                || settings.NoteDisplayMode == ENoteDisplayMode.SentenceBySentence))
        {
            // No need to show a player image and name
            // because it is neither associated with a score nor with lyrics.
            playerImageContainer.HideByDisplay();
            return;
        }

        playerNameLabel.SetTranslatedText(Translation.Of(playerProfile.Name));
        injector.WithRootVisualElement(playerImage)
            .Inject(playerProfileImageControl);
        if (micProfile != null
            || playerProfile is LobbyMemberPlayerProfile)
        {
            playerScoreProgressBar.ShowByDisplay();
            playerScoreProgressBar.ShowByVisibility();
            playerScoreProgressBar.ProgressColor = PlayerColor;
            playerScoreProgressBar.ProgressInPercent = 0;
            playerImageBorder.SetBorderColor(PlayerColor);
        }
        else
        {
            playerScoreProgressBar.HideByVisibility();
            playerImageBorder.HideByVisibility();
            // Do not show border because it looks bad without a fill color
            playerImage.SetBorderWidth(0);
        }

        settings.ObserveEveryValueChanged(it => it.ShowPlayerNames)
            .Subscribe(newValue => playerNameLabel.SetVisibleByDisplay(newValue));

        settings.ObserveEveryValueChanged(it => it.ShowScoreNumbers)
            .Subscribe(newValue => playerScoreLabel.SetVisibleByDisplay(newValue));
    }

    private void ChangeLayoutByPlayerCount()
    {
        if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count >= 5)
        {
            RootVisualElement.AddToClassList("singScenePlayerUiSmall");
        }
    }

    public void Update()
    {
        noteDisplayer.Update();
        playerPitchIndicatorControl.Update();
        UpdateNextPlayerProfileLabel();
    }

    private void UpdateNextPlayerProfileLabel()
    {
        if (nextPlayerProfile == null)
        {
            nextPlayerNameLabel.HideByDisplay();
            return;
        }

        nextPlayerNameLabel.ShowByDisplay();
        Translation newText = Translation.Get(R.Messages.songQueue_nextEntry,
            "value", nextPlayerProfile.Name);
        if (newText.Value != nextPlayerNameLabel.text)
        {
            nextPlayerNameLabel.style.color = new StyleColor(PlayerColor);
            nextPlayerNameLabel.SetTranslatedText(newText);
            AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, nextPlayerNameLabel, setNextPlayerProfileAnimTimeInSeconds);
        }
    }

    private void OnClientConnectionChanged(ClientConnectionChangedEvent connectionChangedEvent)
    {
        if (micProfile == null
            || connectionChangedEvent.CompanionClientHandler.ClientId != micProfile.ConnectedClientId)
        {
            return;
        }

        if (connectionChangedEvent.IsConnected)
        {
            HideMicDisconnectedInfo();
        }
        else
        {
            ShowMicDisconnectedInfo();

            // Trigger achievement
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.disconnectCompanionAppWhenSinging, playerProfile));
        }
    }

    private void HideMicDisconnectedInfo()
    {
        micDisconnectedIcon.HideByVisibility();
    }

    private void ShowMicDisconnectedInfo()
    {
        micDisconnectedIcon.ShowByVisibility();

        // Bouncy size animation
        if (micDisconnectedAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, micDisconnectedAnimationId);
        }

        Vector2 from = Vector2.one * 0.5f;
        micDisconnectedIcon.style.scale = new StyleScale(new Scale(from));
        micDisconnectedAnimationId = LeanTween.value(singSceneControl.gameObject, from, Vector2.one, 0.5f)
            .setEaseSpring()
            .setOnUpdate(s => micDisconnectedIcon.style.scale = new StyleScale(new Scale(new Vector2(s, s))))
            .id;
    }

    public VisualElement ShowSentenceRating(SentenceRating sentenceRating, VisualElement parentContainer)
    {
        if (settings.ScoreMode == EScoreMode.None
            || sentenceRating.PercentageThreshold <= SentenceRating.notBad.PercentageThreshold)
        {
            return null;
        }

        VisualElement visualElement = sentenceRatingUi.CloneTree().Children().First();
        Label label = visualElement.Q<Label>();
        label.SetTranslatedText(sentenceRating.Translation);
        label.style.color = new StyleColor(sentenceRatingColors[sentenceRating.EnumValue]);
        // visualElement.style.unityBackgroundImageTintColor = new StyleColor(sentenceRatingColors[sentenceRating.EnumValue]);
        parentContainer.Add(visualElement);

        visualElement.style.scale = Vector2.zero;
        LeanTween.value(singSceneControl.gameObject, 0, 1, 0.5f)
            .setEaseSpring()
            .setOnUpdate(interpolatedValue => visualElement.style.scale = new Vector2(interpolatedValue, interpolatedValue));

        LeanTween.value(singSceneControl.gameObject, 0, 120, 1.5f)
            .setOnUpdate(interpolatedValue => visualElement.style.bottom = new StyleLength(Length.Percent(interpolatedValue)))
            .setOnComplete(visualElement.RemoveFromHierarchy);
        return visualElement;
    }

    public void ShowTotalScore(int score, bool animate = true)
    {
        if (settings.ScoreMode == EScoreMode.None
            || score == lastDisplayedScore)
        {
            return;
        }

        if (totalScoreAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, totalScoreAnimationId);
        }

        if (score < 0)
        {
            score = 0;
        }

        if (animate)
        {
            totalScoreAnimationId = LeanTween.value(singSceneControl.gameObject, lastDisplayedScore, score, 1f)
                .setOnUpdate((float interpolatedScoreValue) =>
                {
                    playerScoreLabel.SetTranslatedText(Translation.Of(interpolatedScoreValue.ToString("0")));
                    float progressInPercent = (float)(100.0 * interpolatedScoreValue / PlayerScoreControl.maxScore);
                    playerScoreProgressBar.ProgressInPercent = progressInPercent;
                })
                .id;
        }
        else
        {
            playerScoreLabel.SetTranslatedText(Translation.Of(score.ToString("0")));
            float progressInPercent = (float)(100.0 * score / PlayerScoreControl.maxScore);
            playerScoreProgressBar.ProgressInPercent = progressInPercent;
        }

        lastDisplayedScore = score;
    }

    private void CreateMultiplePerfectSentenceEffect()
    {
        if (settings.ScoreMode is EScoreMode.None)
        {
            return;
        }

        EParticleEffect noteAreaEffect = RandomUtils.RandomOfItems(
            EParticleEffect.FireworksEffect2D_Firework5_BlueStar,
            EParticleEffect.FireworksEffect2D_Firework6_YellowStar);
        VfxManager.CreateParticleEffect(new ParticleEffectConfig()
        {
            particleEffect = noteAreaEffect,
            panelPos = noteContainer.worldBound.center,
            scale = 0.4f,
        });
    }

    private void CreateSinglePerfectSentenceEffect()
    {
        if (settings.ScoreMode is EScoreMode.None)
        {
            return;
        }

        VfxManager.CreateParticleEffect(new ParticleEffectConfig()
        {
            particleEffect = EParticleEffect.ShinyItemLoop,
            panelPos = playerImage.worldBound.center,
            scale = 0.2f,
        });
    }

    private void CreatePerfectNoteEffect(Note perfectNote)
    {
        noteDisplayer.CreatePerfectNoteEffect(perfectNote);
    }

    private void InitNoteDisplayer()
    {
        // Find a suited note displayer
        switch (settings.NoteDisplayMode)
        {
            case ENoteDisplayMode.SentenceBySentence:
                noteDisplayer = new SentenceDisplayer();
                break;
            case ENoteDisplayMode.ScrollingNoteStream:
                noteDisplayer = new ScrollingNoteStreamDisplayer();
                break;
            case ENoteDisplayMode.None:
                noteDisplayer = new NoNoteSingSceneDisplayer();
                break;
            default:
                throw new UnityException("Did not find a suited NoteDisplayer for ENoteDisplayMode " + settings.NoteDisplayMode);
        }

        // Enable and initialize the selected note displayer
        injector.Inject(noteDisplayer);
        noteDisplayer.SetLineCount(GetLineCount());
    }

    private int GetLineCount()
    {
        if (settings.NoteDisplayLineCount <= 0)
        {
            List<Note> notes = SongMetaUtils.GetAllNotes(voice);
            int noteRange = SongMetaUtils.GetMaxMidiNote(notes) - SongMetaUtils.GetMinMidiNote(notes);
            int singableNoteRange = MidiUtils.SingableNoteMax - MidiUtils.SingableNoteMin;
            int finalNoteRange = NumberUtils.Limit(noteRange, 0, singableNoteRange);
            return AbstractSingSceneNoteDisplayer.GetLineCount(finalNoteRange + 1);
        }

        return settings.NoteDisplayLineCount;
    }

    public void ShowLeadingPlayerIcon()
    {
        if (!singSceneControl.IsIndividualScore)
        {
            return;
        }

        leadingPlayerIcon.ShowByVisibility();

        // Bouncy size animation
        if (leadingPlayerIconAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, leadingPlayerIconAnimationId);
        }

        Vector2 from = Vector2.one * 0.5f;
        Vector2 to = Vector2.one;
        leadingPlayerIcon.style.scale = new StyleScale(new Scale(from));
        leadingPlayerIconAnimationId = LeanTween.value(singSceneControl.gameObject, from, to, 0.5f)
            .setEaseSpring()
            .setOnUpdate((Vector2 s) => leadingPlayerIcon.style.scale = new StyleScale(new Scale(s)))
            .id;
    }

    public void HideLeadingPlayerIcon()
    {
        leadingPlayerIcon.HideByVisibility();
    }


    public void FadeOutNotes(float animTimeInSeconds)
    {
        noteDisplayer.FadeOut(animTimeInSeconds);
    }

    public void FadeInNotes(float animTimeInSeconds)
    {
        noteDisplayer.FadeIn(animTimeInSeconds);
    }

    // public void FadeOut(float animTimeInSeconds)
    // {
    //     LeanTween.cancel(fadeOutAnimationId);
    //     fadeOutAnimationId = AnimationUtils.FadeOutVisualElement(singSceneControl.gameObject, playerScoreContainer, animTimeInSeconds);
    // }
    //
    // public void FadeIn(float animTimeInSeconds)
    // {
    //     LeanTween.cancel(fadeOutAnimationId);
    //     fadeOutAnimationId = AnimationUtils.FadeInVisualElement(singSceneControl.gameObject, playerScoreContainer, animTimeInSeconds);
    // }

    public void SetPlayerProfile(PlayerProfile newCurrentPlayerProfile)
    {
        if (playerProfile == newCurrentPlayerProfile)
        {
            return;
        }

        playerProfile = newCurrentPlayerProfile;
        playerNameLabel.SetTranslatedText(Translation.Of(newCurrentPlayerProfile.Name));
        playerProfileImageControl.PlayerProfile = newCurrentPlayerProfile;

        // Highlight the change with an animation
        AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, playerScoreLabel, setNextPlayerProfileAnimTimeInSeconds);
        AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, playerNameLabel, setNextPlayerProfileAnimTimeInSeconds);
        AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, playerImageContainer, setNextPlayerProfileAnimTimeInSeconds);
    }

    public void SetNextPlayerProfile(PlayerProfile newNextPlayerProfile)
    {
        if (newNextPlayerProfile == playerProfile)
        {
            // Will keep the current player.
            nextPlayerProfile = null;
        }
        else
        {
            nextPlayerProfile = newNextPlayerProfile;
        }
    }
}
