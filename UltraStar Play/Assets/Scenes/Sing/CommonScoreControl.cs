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

    [Inject(UxmlName = R.UxmlNames.commonScoreContainer)]
    private VisualElement commonScoreContainer;

    [Inject(UxmlName = R.UxmlNames.commonScoreLabel)]
    private Label commonScoreLabel;

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
        else
        {
            commonScoreContainer.HideByDisplay();
        }
    }

    private void InitCommonScore()
    {
        commonScoreContainer.ShowByDisplay();
        commonScoreLabel.text = "0";
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
    }

    private void ShowSentenceRating(SentenceRating sentenceRating)
    {
        PlayerControl playerControl = singSceneControl.PlayerControls.FirstOrDefault();
        VisualElement sentenceRatingVisualElement = playerControl.PlayerUiControl.ShowSentenceRating(sentenceRating, commonScoreSentenceRatingContainer);
        if (sentenceRatingVisualElement != null)
        {
            sentenceRatingVisualElement.style.position = new StyleEnum<Position>(Position.Relative);
        }
    }

    private void UpdateCommonScoreLabel()
    {
        double commonScore = ScoreControls.Select(scoreControl => scoreControl.TotalScore).Average();
        ShowTotalScore((int)commonScore);
    }

    private void ShowTotalScore(int score)
    {
        if (totalScoreAnimationId > 0)
        {
            LeanTween.cancel(singSceneControl.gameObject, totalScoreAnimationId);
        }

        if (!int.TryParse(commonScoreLabel.text, out int lastDisplayedScore)
            || lastDisplayedScore < 0)
        {
            lastDisplayedScore = 0;
        }
        if (score < 0)
        {
            score = 0;
        }
        totalScoreAnimationId = LeanTween.value(singSceneControl.gameObject, lastDisplayedScore, score, 1f)
            .setOnUpdate((float interpolatedScoreValue) => commonScoreLabel.text = interpolatedScoreValue.ToString("0"))
            .id;
    }
}
