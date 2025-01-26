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

    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.commonScoreSentenceRatingContainer)]
    private VisualElement commonScoreSentenceRatingContainer;

    private IEnumerable<PlayerScoreControl> ScoreControls => singSceneControl.PlayerControls
        .Select(playerControl => playerControl.PlayerScoreControl)
        .Where(scoreControl => scoreControl != null);

    private IEnumerable<PlayerPerformanceAssessmentControl> PerformanceAssessmentControls => singSceneControl.PlayerControls
        .Select(playerControl => playerControl.PlayerPerformanceAssessmentControl)
        .Where(control => control != null);

    private int totalScoreAnimationId;

    private readonly HashSet<Sentence> ratedSentences = new();

    public void OnInjectionFinished()
    {
        commonScoreSentenceRatingContainer.Clear();
        if (singSceneControl.IsCommonScore)
        {
            if (singSceneControl.PlayerControls.IsNullOrEmpty())
            {
                AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () => InitCommonScore());
            }
            else
            {
                InitCommonScore();
            }
        }
    }

    private void InitCommonScore()
    {
        ScoreControls.ForEach(scoreControl =>
        {
            scoreControl.ScoreChangedEventStream
                .Subscribe(_ => UpdateCommonScoreLabel())
                .AddTo(gameObject);
        });

        // Show only "friendly" sentence ratings.
        PerformanceAssessmentControls.Select(scoreControl => scoreControl.SentenceAssessedEventStream)
            .Merge()
            .Subscribe(sentenceScoreEvent =>
            {
                if (ratedSentences.Contains(sentenceScoreEvent.Sentence)
                    || sentenceScoreEvent.SentenceRating.PercentageThreshold < 0.25)
                {
                    return;
                }

                ratedSentences.Add(sentenceScoreEvent.Sentence);
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
        double commonScore = 0;
        List<int> scores = ScoreControls.Select(scoreControl => scoreControl.TotalScore).ToList();
        if (!scores.IsNullOrEmpty())
        {
            commonScore = scores.Average();
        }

        singSceneControl.PlayerControls.ForEach(playerControl => playerControl.PlayerUiControl.ShowTotalScore((int)commonScore, animate));
    }
}
