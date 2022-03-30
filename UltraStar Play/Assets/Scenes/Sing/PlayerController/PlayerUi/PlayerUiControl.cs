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

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement RootVisualElement { get; private set; }

    [Inject]
    private PlayerScoreController playerScoreController;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject(Key = nameof(sentenceRatingUi))]
    private VisualTreeAsset sentenceRatingUi;

    [Inject(UxmlName = R.UxmlNames.sentenceRatingContainer)]
    private VisualElement sentenceRatingContainer;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject(UxmlName = R.UxmlNames.scoreContainer)]
    private VisualElement scoreContainer;

    [Inject(UxmlName = R.UxmlNames.scoreLabel)]
    private Label scoreLabel;

    [Inject(UxmlName = R.UxmlNames.micDisconnectedContainer)]
    private VisualElement micDisconnectedContainer;

    [Inject(UxmlName = R.UxmlNames.playerImage)]
    private VisualElement playerImage;

    [Inject(UxmlName = R.UxmlNames.playerNameLabel)]
    private Label playerNameLabel;

    [Inject(UxmlName = R.UxmlNames.leadingPlayerIcon)]
    private VisualElement leadingPlayerIcon;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private SingSceneControl singSceneControl;

    private AbstractSingSceneNoteDisplayer noteDisplayer;

    private int totalScoreAnimationId;
    private int micDisconnectedAnimationId;
    private int leadingPlayerIconAnimationId;

    public void OnInjectionFinished()
    {
        InitNoteDisplayer(LineCount);

        // Player name and image
        playerNameLabel.text = playerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .CreateAndInject<AvatarImageControl>();

        if (micProfile != null)
        {
            scoreContainer.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
        }

        HideLeadingPlayerIcon();

        // Show rating and score after each sentence
        if (settings.GameSettings.RatePlayers)
        {
            scoreLabel.text = "";
            ShowTotalScore(playerScoreController.TotalScore);
            playerScoreController.SentenceScoreEventStream.Subscribe(sentenceScoreEvent =>
            {
                ShowTotalScore(playerScoreController.TotalScore);
                ShowSentenceRating(sentenceScoreEvent.SentenceRating);
            });
        }
        else
        {
            scoreContainer.HideByDisplay();
        }

        // Show an effect for perfectly sung notes
        playerScoreController.NoteScoreEventStream.Subscribe(noteScoreEvent =>
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

        // Create effect when there are at least two perfect sentences in a row.
        // Therefor, consider the currently finished sentence and its predecessor.
        playerScoreController.SentenceScoreEventStream.Buffer(2, 1)
            // All elements (i.e. the currently finished and its predecessor) must have been "perfect"
            .Where(xs => xs.AllMatch(x => x.SentenceRating == SentenceRating.perfect))
            // Create an effect for these.
            .Subscribe(xs => CreatePerfectSentenceEffect());

        ChangeLayoutByPlayerCount();
    }

    private void ChangeLayoutByPlayerCount()
    {
        if (singSceneControl.SceneData.SelectedPlayerProfiles.Count >= 5)
        {
            RootVisualElement.AddToClassList("singScenePlayerUiSmall");
        }
    }

    public void Update()
    {
        noteDisplayer.Update();
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
        micDisconnectedContainer.HideByVisibility();
    }

    private void ShowMicDisconnectedInfo()
    {
        micDisconnectedContainer.ShowByVisibility();
        micDisconnectedContainer.Q<Label>().text = "Mic Disconnected";

        // Bouncy size animation
        if (micDisconnectedAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, micDisconnectedAnimationId);
        }

        Vector3 from = Vector3.one * 0.5f;
        micDisconnectedContainer.style.scale = new StyleScale(new Scale(from));
        micDisconnectedAnimationId = LeanTween.value(singSceneControl.gameObject, from, Vector3.one, 0.5f)
            .setEaseSpring()
            .setOnUpdate(s => micDisconnectedContainer.style.scale = new StyleScale(new Scale(new Vector3(s, s, s))))
            .id;
    }

    private void ShowSentenceRating(SentenceRating sentenceRating)
    {
        if (!settings.GameSettings.RatePlayers)
        {
            return;
        }

        VisualElement visualElement = sentenceRatingUi.CloneTree().Children().First();
        visualElement.Q<Label>().text = sentenceRating.Text;
        visualElement.style.unityBackgroundImageTintColor = sentenceRating.BackgroundColor;
        visualElement.style.right = 0;
        sentenceRatingContainer.Add(visualElement);

        // Animate moving upwards, then destroy
        float visualElementHeight = 30;
        visualElement.style.top = visualElementHeight;
        LeanTween.value(singSceneControl.gameObject, visualElementHeight, 0, 1f)
            .setEaseInSine()
            .setOnUpdate(interpolatedTop => visualElement.style.top = interpolatedTop)
            .setOnComplete(visualElement.RemoveFromHierarchy);
    }

    private void ShowTotalScore(int score)
    {
        if (!settings.GameSettings.RatePlayers)
        {
            return;
        }

        if (totalScoreAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, totalScoreAnimationId);
        }

        if (!int.TryParse(scoreLabel.text, out int lastDisplayedScore)
            || lastDisplayedScore < 0)
        {
            lastDisplayedScore = 0;
        }
        if (score < 0)
        {
            score = 0;
        }
        totalScoreAnimationId = LeanTween.value(singSceneControl.gameObject, lastDisplayedScore, score, 1f)
            .setOnUpdate((float interpolatedScoreValue) => scoreLabel.text = interpolatedScoreValue.ToString("0"))
            .id;
    }

    private void CreatePerfectSentenceEffect()
    {
        noteDisplayer.CreatePerfectSentenceEffect();
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
            default:
                throw new UnityException("Did not find a suited NoteDisplayer for ENoteDisplayMode " + settings.GraphicSettings.noteDisplayMode);
        }

        // Enable and initialize the selected note displayer
        injector.Inject(noteDisplayer);
        noteDisplayer.SetLineCount(localLineCount);
    }

    public void ShowLeadingPlayerIcon()
    {
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
}
