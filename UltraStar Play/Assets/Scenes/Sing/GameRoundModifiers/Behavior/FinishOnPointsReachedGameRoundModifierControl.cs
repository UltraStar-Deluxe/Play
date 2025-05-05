using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class FinishOnPointsReachedGameRoundModifierControl : GameRoundModifierControl
{
    public int pointsThreshold;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneFinisher singSceneFinisher;

    [Inject]
    private SingSceneData singSceneData;

    private bool isFinished;

    private void Start()
    {
        singSceneControl.PlayerControls
            .Select(playerControl => playerControl.PlayerScoreControl.ScoreChangedEventStream)
            .Merge()
            .Subscribe(evt => UpdateFinish())
            .AddTo(gameObject);
    }

    private void UpdateFinish()
    {
        if (isFinished
            || singSceneData.IsMedley)
        {
            return;
        }

        PlayerControl playerControlAbovePointsThreshold = singSceneControl
            .PlayerControls
            .FirstOrDefault(playerControl => playerControl.PlayerScoreControl.TotalScore >= pointsThreshold);
        if (playerControlAbovePointsThreshold != null)
        {
            Debug.Log($"Triggering finish because player '{playerControlAbovePointsThreshold.PlayerProfile?.Name}' has more than {pointsThreshold} points");
            singSceneFinisher.TriggerEarlySongFinish();
        }
    }
}
