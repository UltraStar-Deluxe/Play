using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class FinishOnAdvanceOfPointsReachedGameRoundModifierControl : GameRoundModifierControl
    {
        public int pointsThreshold;

        [Inject]
        private SingSceneControl singSceneControl;

        [Inject]
        private SingSceneFinisher singSceneFinisher;

        [Inject]
        private SingSceneData singSceneData;

        private bool isFinished;

        private List<PlayerControl> PlayerControls => singSceneControl.PlayerControls;

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

            PlayerControl firstPlayer = PlayerControls
                .FindMaxElement(it => it.PlayerScoreControl.TotalScore);
            if (firstPlayer == null
                || firstPlayer.PlayerScoreControl.TotalScore <= 0)
            {
                return;
            }

            PlayerControl secondPlayer = PlayerControls
                .Except(new List<PlayerControl>() { firstPlayer })
                .FindMaxElement(it => it.PlayerScoreControl.TotalScore);
            if (secondPlayer == null)
            {
                return;
            }

            int scoreDistance = Math.Abs(firstPlayer.PlayerScoreControl.TotalScore
                                         - secondPlayer.PlayerScoreControl.TotalScore);
            if (scoreDistance > pointsThreshold)
            {
                string firstPlayerName = firstPlayer.PlayerProfile?.Name;
                string secondPlayerName = secondPlayer.PlayerProfile?.Name;
                Debug.Log($"Triggering finish because of player advance " +
                          $"(first: {firstPlayerName}: {firstPlayer.PlayerScoreControl.TotalScore}, " +
                          $"second: {secondPlayerName}: {secondPlayer.PlayerScoreControl.TotalScore}, " +
                          $"score distance: {scoreDistance}," +
                          $"score distance threshold: {pointsThreshold})");
                singSceneFinisher.TriggerEarlySongFinish();
            }
        }
    }
