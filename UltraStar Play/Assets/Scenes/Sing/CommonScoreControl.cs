using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class CommonScoreControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject(UxmlName = R.UxmlNames.commonScoreSentenceRatingContainer)]
    private VisualElement commonScoreSentenceRatingContainer;

    private IEnumerable<PlayerScoreControl> ScoreControls => singSceneControl.PlayerControls
        .Select(playerControl => playerControl.PlayerScoreControl);

    private int totalScoreAnimationId;

    private readonly HashSet<Sentence> ratedSentences = new();

    public void OnInjectionFinished()
    {
        commonScoreSentenceRatingContainer.Clear();
        if (singSceneControl.IsCommonScore)
        {
            InitCommonScore();
        }
    }

    private void InitCommonScore()
    {
        ScoreControls.ForEach(scoreControl =>
        {
            scoreControl.SentenceScoreEventStream
                .Subscribe(_ => UpdateCommonScoreLabel())
                .AddTo(singSceneControl.gameObject);
        });

        // Show only "friendly" sentence ratings.
        ScoreControls.Select(scoreControl => scoreControl.SentenceScoreEventStream)
            .Merge()
            .Subscribe(sentenceScoreEvent =>
            {
                if (ratedSentences.Contains(sentenceScoreEvent.SentenceScore.Sentence)
                    || sentenceScoreEvent.SentenceRating.PercentageThreshold <= 0.5)
                {
                    return;
                }

                ratedSentences.Add(sentenceScoreEvent.SentenceScore.Sentence);
                ShowSentenceRating(sentenceScoreEvent.SentenceRating);
            })
            .AddTo(singSceneControl.gameObject);
        
        UpdateCommonScoreLabel(false);
    }

    private void ShowSentenceRating(SentenceRating sentenceRating)
    {
        if (sentenceRating == SentenceRating.bad)
        {
            return;
        }

        PlayerControl playerControl = singSceneControl.PlayerControls.FirstOrDefault();
        VisualElement sentenceRatingVisualElement = playerControl.PlayerUiControl.ShowSentenceRating(sentenceRating, commonScoreSentenceRatingContainer);
        if (sentenceRatingVisualElement != null)
        {
            sentenceRatingVisualElement.style.position = new StyleEnum<Position>(Position.Relative);
        }
    }

    private void UpdateCommonScoreLabel(bool animate = true)
    {
        double commonScore = ScoreControls.Select(scoreControl => scoreControl.TotalScore).Average();
        singSceneControl.PlayerControls.ForEach(playerControl => playerControl.PlayerUiControl.ShowTotalScore((int)commonScore, animate));
    }
}
