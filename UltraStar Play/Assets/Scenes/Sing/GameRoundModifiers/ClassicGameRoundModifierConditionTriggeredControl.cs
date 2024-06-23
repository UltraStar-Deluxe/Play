using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;

public abstract class ClassicConditionGameRoundModifierControl : GameRoundModifierControl
{
    public ClassicGameRoundModifierConditionSettings conditionSettings;

    [Inject]
    protected SingSceneControl singSceneControl;

    [Inject]
    protected SingSceneMedleyControl medleyControl;

    private List<PlayerControl> PlayerControls => singSceneControl.PlayerControls;

    public abstract void ActivateModifier(IReadOnlyCollection<PlayerControl> playerControls);
    public abstract void DeactivateModifier(IReadOnlyCollection<PlayerControl> playerControls);

    private readonly HashSet<PlayerControl> playerControlsWithActiveModifier = new();

    public void Update()
    {
        if (conditionSettings == null
            || conditionSettings.condition is EClassicGameRoundModifierCondition.Always)
        {
            ActivateModifierIfNotDoneYet(PlayerControls);
        }
        else if (conditionSettings.condition is EClassicGameRoundModifierCondition.TimeRange)
        {
            double timeInSongInPercent = medleyControl.CurrentTimeInSongInPercentConsideringMedley;
            bool timeRangeMatches = conditionSettings.IsValueInRange(timeInSongInPercent * 100);
            if (timeRangeMatches)
            {
                ActivateModifierIfNotDoneYet(PlayerControls);
            }
            else
            {
                DeactivateModifierIfNotDoneYet(PlayerControls);
            }
        }
        else if (conditionSettings.condition is EClassicGameRoundModifierCondition.ScoreRange)
        {
            List<PlayerControl> matchingPlayerControls = PlayerControls
                .Where(playerControl =>
                {
                    double totalScoreInPercent = 100.0 * (double)playerControl.PlayerScoreControl.TotalScore /
                        (double)PlayerScoreControl.maxScore;
                    return conditionSettings.IsValueInRange(totalScoreInPercent);
                })
                .ToList();
            List<PlayerControl> notMatchingPlayerControls = PlayerControls
                .Except(matchingPlayerControls)
                .ToList();
            ActivateModifierIfNotDoneYet(matchingPlayerControls);
            DeactivateModifierIfNotDoneYet(notMatchingPlayerControls);
        }
        else if (conditionSettings.condition is EClassicGameRoundModifierCondition.PlayerAdvance)
        {
            int scoreDistanceThreshold = (int)((conditionSettings.conditionRangePercent.x / 100.0) * PlayerScoreControl.maxScore);
            List<PlayerControl> matchingPlayerControls = GetPlayerControlsWithScoreDistanceToNextPlayer(scoreDistanceThreshold);
            List<PlayerControl> notMatchingPlayerControls = PlayerControls
                .Except(matchingPlayerControls)
                .ToList();
            ActivateModifierIfNotDoneYet(matchingPlayerControls);
            DeactivateModifierIfNotDoneYet(notMatchingPlayerControls);
        }
    }

    private List<PlayerControl> GetPlayerControlsWithScoreDistanceToNextPlayer(int scoreDistanceThreshold)
    {
        List<PlayerControl> result = new();

        List<PlayerControl> playerControlsOrderedByScore = PlayerControls
            .OrderBy(playerControl => -playerControl.PlayerScoreControl.TotalScore)
            .ToList();

        PlayerControl lastPlayerControl = null;
        foreach (PlayerControl playerControl in playerControlsOrderedByScore)
        {
            if (lastPlayerControl != null)
            {
                int scoreDistance = Math.Abs(lastPlayerControl.PlayerScoreControl.TotalScore
                                             - playerControl.PlayerScoreControl.TotalScore);
                if (scoreDistance >= scoreDistanceThreshold)
                {
                    result.Add(lastPlayerControl);
                }
            }

            lastPlayerControl = playerControl;
        }

        return result;
    }

    private void ActivateModifierIfNotDoneYet(List<PlayerControl> playerControls)
    {
        if (playerControls.IsNullOrEmpty())
        {
            return;
        }

        List<PlayerControl> relevantPlayerControls = playerControls
            .Except(playerControlsWithActiveModifier)
            .ToList();
        if (relevantPlayerControls.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            ActivateModifier(relevantPlayerControls);
        }
        finally
        {
            playerControlsWithActiveModifier.AddRange(relevantPlayerControls);
        }
    }

    private void DeactivateModifierIfNotDoneYet(List<PlayerControl> playerControls)
    {
        if (playerControls.IsNullOrEmpty())
        {
            return;
        }

        List<PlayerControl> relevantPlayerControls = playerControlsWithActiveModifier
            .Intersect(playerControls)
            .ToList();
        if (relevantPlayerControls.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            DeactivateModifier(relevantPlayerControls);
        }
        finally
        {
            playerControlsWithActiveModifier.RemoveRange(playerControls);
        }
    }
}
