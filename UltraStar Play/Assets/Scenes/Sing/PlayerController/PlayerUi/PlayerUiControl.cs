using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiControl : INeedInjection, IInjectionFinishedListener
{
    private const int LineCount = 10;

    [Inject(Key = "playerUi")]
    private VisualTreeAsset playerUi;

    [Inject]
    private PlayerScoreController playerScoreController;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private PlayerProfile playerProfile;

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

    // private SentenceRatingDisplayer sentenceRatingDisplayer;

    private PlayerNameText playerNameText;

    private AvatarImage avatarImage;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private SingSceneControl singSceneControl;

    // private ISingSceneNoteDisplayer noteDisplayer;

    private int totalScoreAnimationId;
    private int micDisconnectedAnimationId;
    private int leadingPlayerIconAnimationId;

    public void OnInjectionFinished()
    {
        // InitNoteDisplayer(LineCount);

        // Player name and image
        playerNameLabel.text = playerProfile.Name;
        AvatarImageControl avatarImageControl = new AvatarImageControl();
        injector.WithRootVisualElement(playerImage)
            .Inject(avatarImageControl);

        HideLeadingPlayerIcon();

        // Show rating and score after each sentence
        scoreLabel.text = "";
        ShowTotalScore(playerScoreController.TotalScore);
        playerScoreController.SentenceScoreEventStream.Subscribe(sentenceScoreEvent =>
        {
            ShowTotalScore(playerScoreController.TotalScore);
            ShowSentenceRating(sentenceScoreEvent.SentenceRating);
        });

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
            .Where(xs => xs.AllMatch(x => x.SentenceRating == SentenceRating.Perfect))
            // Create an effect for these.
            .Subscribe(xs => CreatePerfectSentenceEffect());
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

    public void ShowSentenceRating(SentenceRating sentenceRating)
    {
        // sentenceRatingDisplayer.ShowSentenceRating(sentenceRating);
    }

    public void ShowTotalScore(int score)
    {
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

    public void CreatePerfectSentenceEffect()
    {
        // noteDisplayer.CreatePerfectSentenceEffect();
    }

    public void CreatePerfectNoteEffect(Note perfectNote)
    {
        // noteDisplayer.CreatePerfectNoteEffect(perfectNote);
    }

    private void InitNoteDisplayer(int localLineCount)
    {
        // // Find a suited note displayer
        // if (settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.SentenceBySentence)
        // {
        //     noteDisplayer = new SentenceDisplayer();
        // }
        // else if (settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.ScrollingNoteStream)
        // {
        //     noteDisplayer = new ScrollingNoteStreamDisplayer();
        // }
        // if (noteDisplayer == null)
        // {
        //     throw new UnityException("Did not find a suited ISingSceneNoteDisplayer for ENoteDisplayMode " + settings.GraphicSettings.noteDisplayMode);
        // }
        //
        // // Enable and initialize the selected note displayer
        // noteDisplayer.GetGameObject().SetActive(true);
        // injector.InjectAllComponentsInChildren(noteDisplayer.GetGameObject());
        // noteDisplayer.Init(localLineCount);
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

        Vector3 from = Vector3.one * 0.5f;
        leadingPlayerIcon.style.scale = new StyleScale(new Scale(from));
        leadingPlayerIconAnimationId = LeanTween.value(singSceneControl.gameObject, from, Vector3.one, 0.5f)
            .setEaseSpring()
            .setOnUpdate(s => leadingPlayerIcon.style.scale = new StyleScale(new Scale(new Vector3(s, s, s))))
            .id;
    }

    public void HideLeadingPlayerIcon()
    {
        leadingPlayerIcon.HideByVisibility();
    }
}
