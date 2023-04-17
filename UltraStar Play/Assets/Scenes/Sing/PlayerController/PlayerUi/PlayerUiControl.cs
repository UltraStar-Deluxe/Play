using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiControl : INeedInjection, IInjectionFinishedListener
{
    private const int LineCount = 10;

    [Inject]
    private PlayerScoreControl playerScoreControl;
    
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

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneData sceneData;

    private AbstractSingSceneNoteDisplayer noteDisplayer;

    private PlayerProfile nextPlayerProfile;
    private float displayNextPlayerProfileTimeInSeconds;
    
    private int totalScoreAnimationId;
    private int micDisconnectedAnimationId;
    private int leadingPlayerIconAnimationId;
    private int fadeOutAnimationId;
    
    private Dictionary<ESentenceRating, Color32> sentenceRatingColors;

    private readonly PlayerProfileImageControl playerProfileImageControl = new();
    private readonly PlayerPitchIndicatorControl playerPitchIndicatorControl = new();
    
    private float setNextPlayerProfileAnimTimeInSeconds = 1.5f;
    
    public void OnInjectionFinished()
    {
        InitPlayerNameAndImage();
        InitNoteDisplayer(LineCount);

        injector
            .WithBindingForInstance(noteDisplayer)
            .Inject(playerPitchIndicatorControl);

        // Show rating and score after each sentence
        if (singSceneControl.IsIndividualScore)
        {
            playerScoreLabel.text = "";
            ShowTotalScore(playerScoreControl.TotalScore);
            playerScoreControl.SentenceScoreEventStream.Subscribe(sentenceScoreEvent =>
            {
                ShowTotalScore(playerScoreControl.TotalScore);
                ShowSentenceRating(sentenceScoreEvent.SentenceRating, sentenceRatingContainer);
            });
        }

        // Show an effect for perfectly sung notes
        playerScoreControl.NoteScoreEventStream.Subscribe(noteScoreEvent =>
        {
            if (noteScoreEvent.NoteScore.IsPerfect)
            {
                CreatePerfectNoteEffect(noteScoreEvent.NoteScore.Note);
            }
        });

        // Show message when mic (dis)connects.
        HideMicDisconnectedInfo();
        if (micProfile != null
            && micProfile.IsInputFromConnectedClient)
        {
            serverSideConnectRequestManager.ClientConnectedEventStream
                .Subscribe(HandleClientConnectedEvent)
                .AddTo(singSceneControl);
        }

        nextPlayerNameLabel.HideByDisplay();

        // Create effect when there are at least two perfect sentences in a row.
        // Therefor, consider the currently finished sentence and its predecessor.
        playerScoreControl.SentenceScoreEventStream.Buffer(2, 1)
            // All elements (i.e. the currently finished and its predecessor) must have been "perfect"
            .Where(xs => xs.AllMatch(x => x.SentenceRating == SentenceRating.perfect))
            // Create an effect for these.
            .Subscribe(xs => CreateMultiplePerfectSentenceEffect());

        // Single perfect sentence effect
        playerScoreControl.SentenceScoreEventStream
            .Where(x => x.SentenceRating == SentenceRating.perfect)
            .Subscribe(xs => CreateSinglePerfectSentenceEffect());
        
        ChangeLayoutByPlayerCount();

        sentenceRatingColors = themeManager.GetSentenceRatingColors();
    }

    private void InitPlayerNameAndImage()
    {
        HideLeadingPlayerIcon();

        if (settings.GameSettings.ScoreMode == EScoreMode.None
            && (settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.None
                || settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.SentenceBySentence))
        {
            // No need to show a player image and name
            // because it is neither associated with a score nor with lyrics.
            playerImageContainer.HideByDisplay();
            return;
        }

        playerNameLabel.text = playerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .Inject(playerProfileImageControl);
        if (micProfile != null)
        {
            playerScoreProgressBar.ShowByDisplay();
            playerScoreProgressBar.ShowByVisibility();
            playerScoreProgressBar.ProgressColor = micProfile.Color;
            playerImageBorder.SetBorderColor(micProfile.Color);
        }
        else
        {
            playerScoreProgressBar.HideByVisibility();
            playerImageBorder.HideByVisibility();
        }

        if (!settings.GraphicSettings.showPlayerNames)
        {
            playerNameLabel.HideByDisplay();
        }
        if (!settings.GraphicSettings.showScoreNumbers)
        {
            playerScoreLabel.HideByDisplay();
        }
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
        string newText = $"Next: {nextPlayerProfile.Name}";
        if (newText != nextPlayerNameLabel.text)
        {
            if (micProfile != null)
            {
                nextPlayerNameLabel.style.color = new StyleColor(micProfile.Color);
            }
            nextPlayerNameLabel.text = newText;
            AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, nextPlayerNameLabel, setNextPlayerProfileAnimTimeInSeconds);
        }
    }

    private void HandleClientConnectedEvent(ClientConnectionEvent connectionEvent)
    {
        if (micProfile == null
            || connectionEvent.ConnectedClientHandler.ClientId != micProfile.ConnectedClientId)
        {
            return;
        }
        
        if (connectionEvent.IsConnected)
        {
            HideMicDisconnectedInfo();
        }
        else
        {
            ShowMicDisconnectedInfo();
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

        Vector3 from = Vector3.one * 0.5f;
        micDisconnectedIcon.style.scale = new StyleScale(new Scale(from));
        micDisconnectedAnimationId = LeanTween.value(singSceneControl.gameObject, from, Vector3.one, 0.5f)
            .setEaseSpring()
            .setOnUpdate(s => micDisconnectedIcon.style.scale = new StyleScale(new Scale(new Vector3(s, s, s))))
            .id;
    }

    public VisualElement ShowSentenceRating(SentenceRating sentenceRating, VisualElement parentContainer)
    {
        if (settings.GameSettings.ScoreMode == EScoreMode.None
            || sentenceRating.PercentageThreshold <= SentenceRating.notBad.PercentageThreshold)
        {
            return null;
        }

        VisualElement visualElement = sentenceRatingUi.CloneTree().Children().First();
        Label label = visualElement.Q<Label>();
        label.text = sentenceRating.Text;
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
        if (settings.GameSettings.ScoreMode == EScoreMode.None)
        {
            return;
        }

        if (totalScoreAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, totalScoreAnimationId);
        }

        if (!int.TryParse(playerScoreLabel.text, out int lastDisplayedScore)
            || lastDisplayedScore < 0)
        {
            lastDisplayedScore = 0;
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
                    playerScoreLabel.text = interpolatedScoreValue.ToString("0");
                    playerScoreProgressBar.ProgressInPercent = (float)(100.0 * interpolatedScoreValue / PlayerScoreControl.maxScore);
                })
                .id;
        }
        else
        {
            playerScoreLabel.text = score.ToString("0");
            playerScoreProgressBar.ProgressInPercent = (float)(100.0 * score / PlayerScoreControl.maxScore);
        }
    }

    private void CreateMultiplePerfectSentenceEffect()
    {
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

    private void InitNoteDisplayer(int localLineCount)
    {
        // Find a suited note displayer
        switch (settings.GraphicSettings.noteDisplayMode)
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
                throw new UnityException("Did not find a suited NoteDisplayer for ENoteDisplayMode " + settings.GraphicSettings.noteDisplayMode);
        }

        // Enable and initialize the selected note displayer
        injector.Inject(noteDisplayer);
        noteDisplayer.SetLineCount(localLineCount);
    }

    public void ShowLeadingPlayerIcon()
    {
        if (!singSceneControl.IsIndividualScore)
        {
            return;
        }

        leadingPlayerIcon.ShowByVisibility();
        if (micProfile != null)
        {
            leadingPlayerIcon.style.color = new StyleColor(micProfile.Color);
        }

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
        playerNameLabel.text = newCurrentPlayerProfile.Name;
        playerProfileImageControl.PlayerProfile = newCurrentPlayerProfile;

        // Highlight the change with an animation
        AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, playerNameLabel, setNextPlayerProfileAnimTimeInSeconds);
        AnimationUtils.BounceVisualElementSize(singSceneControl.gameObject, playerImage, setNextPlayerProfileAnimTimeInSeconds);
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
