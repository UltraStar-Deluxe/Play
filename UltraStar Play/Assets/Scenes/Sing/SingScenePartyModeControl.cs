using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingScenePartyModeControl : INeedInjection
{
    private const float AnimTimeInSeconds = 1.5f;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneFinisher singSceneFinisher;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public ReactiveProperty<int> ModifiedVolumePercent { get; private set; } = new(100);

    private readonly HashSet<EGameRoundModifier> activeModifiers = new();

    public void Update()
    {
        if (!singSceneControl.HasPartyModeSettings)
        {
            return;
        }

        UpdateFinishCondition();
        UpdateModifiers();
    }

    private void UpdateFinishCondition()
    {
        if (IsFinishConditionTriggered())
        {
            singSceneFinisher.TriggerEarlySongFinish();
        }
    }

    private bool IsFinishConditionTriggered()
    {
        EGameRoundFinishCondition finishCondition = singSceneControl.PartyModeSettings.CurrentRoundSettings.finishConditionSettings.condition;
        int finishConditionScore = singSceneControl.PartyModeSettings.CurrentRoundSettings.finishConditionSettings.points;
        if (finishCondition == EGameRoundFinishCondition.ReachPoints)
        {
            return singSceneControl.PlayerControls.AnyMatch(playerControl
                => playerControl.PlayerScoreControl.TotalScore >= finishConditionScore);
        }

        if (finishCondition == EGameRoundFinishCondition.ReachAdvanceOfPoints)
        {
            if (singSceneControl.PlayerControls.Count <= 1)
            {
                // Cannot reach an advance with only one player
                return false;
            }

            return GetFirstPlayerScoreDistanceToSecondPlayer() > finishConditionScore;
        }

        // "Reach end of song" => Use normal SingSceneFinisher
        return false;
    }

    private void UpdateModifiers()
    {
        singSceneControl.PlayerControls.ForEach(playerControl => UpdatePlayerSpecificModifiers(playerControl));
        UpdatePlayerIndependentModifiers();
    }

    private void UpdatePlayerIndependentModifiers()
    {
        HashSet<EGameRoundModifier> modifiers = singSceneControl.PartyModeSettings.CurrentRoundSettings.modifiers;
        bool isModifierConditionTriggered = singSceneControl.PlayerControls
            .AnyMatch(playerControl => IsModifierConditionTriggered(playerControl));
        if (isModifierConditionTriggered)
        {
            if (modifiers.Contains(EGameRoundModifier.ReduceAudio))
            {
                ReduceAudio();
            }
        }
        else
        {
            if (modifiers.Contains(EGameRoundModifier.ReduceAudio))
            {
                NormalizeAudio();
            }
        }

        if (modifiers.Contains(EGameRoundModifier.PassTheMic))
        {
            UpdatePassTheMic();
        }
    }

    private void NormalizeAudio()
    {
        ModifiedVolumePercent.Value = 100;
    }

    private void ReduceAudio()
    {
        ModifiedVolumePercent.Value = 3;
    }

    private void UpdatePassTheMic()
    {

    }

    private void UpdatePlayerSpecificModifiers(PlayerControl playerControl)
    {
        HashSet<EGameRoundModifier> modifiers = singSceneControl.PartyModeSettings.CurrentRoundSettings.modifiers;
        bool isModifierConditionTriggered = IsModifierConditionTriggered(playerControl);
        modifiers.ForEach(modifier =>
        {
            if (isModifierConditionTriggered)
            {
                if (!activeModifiers.Contains(modifier))
                {
                    activeModifiers.Add(modifier);
                    ActivateModifier(playerControl, modifier);
                }
            }
            else
            {
                if (activeModifiers.Contains(modifier))
                {
                    activeModifiers.Remove(modifier);
                    DeactivateModifier(playerControl, modifier);
                }
            }
        });
    }

    private void ActivateModifier(PlayerControl playerControl, EGameRoundModifier modifier)
    {
        if (modifier == EGameRoundModifier.HideLyrics)
        {
            singSceneControl.FadeOutLyrics(playerControl.Voice, AnimTimeInSeconds);
        }
        else if (modifier == EGameRoundModifier.HideNotes)
        {
            playerControl.PlayerUiControl.FadeOutNotes(AnimTimeInSeconds);
        }
        else if (modifier == EGameRoundModifier.HideScore)
        {
            playerControl.PlayerUiControl.FadeOut(AnimTimeInSeconds);
        }
    }

    private void DeactivateModifier(PlayerControl playerControl, EGameRoundModifier modifier)
    {
        if (modifier == EGameRoundModifier.HideLyrics)
        {
            singSceneControl.FadeInLyrics(playerControl.Voice, AnimTimeInSeconds);
        }
        else if (modifier == EGameRoundModifier.HideNotes)
        {
            playerControl.PlayerUiControl.FadeInNotes(AnimTimeInSeconds);
        }
        else if (modifier == EGameRoundModifier.HideScore)
        {
            playerControl.PlayerUiControl.FadeIn(AnimTimeInSeconds);
        }
    }

    private bool IsModifierConditionTriggered(PlayerControl playerControl)
    {
        GameRoundModifierConditionSettings modifierConditionSettings = singSceneControl.PartyModeSettings.CurrentRoundSettings.modifierConditionSettings;
        if (modifierConditionSettings.condition == EGameRoundModifierCondition.Always)
        {
            return true;
        }

        if (modifierConditionSettings.condition == EGameRoundModifierCondition.ScoreRange)
        {
            return modifierConditionSettings.scoreFrom <= playerControl.PlayerScoreControl.TotalScore
                   && playerControl.PlayerScoreControl.TotalScore <= modifierConditionSettings.scoreUntil;
        }

        if (modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
        {
            int positionInSongInPercent = (int)(songAudioPlayer.PositionInSongInPercent * 100);
            return modifierConditionSettings.timeFrom <= positionInSongInPercent
                   && positionInSongInPercent <= modifierConditionSettings.timeUntil;
        }

        if (modifierConditionSettings.condition == EGameRoundModifierCondition.PlayerAdvance)
        {
            return GetFirstPlayerScoreDistanceToSecondPlayer() >= modifierConditionSettings.scoreFrom;
        }

        return false;
    }

    private int GetFirstPlayerScoreDistanceToSecondPlayer()
    {
        PlayerControl firstPlayerControl = singSceneControl.PlayerControls
            .FindMaxElement(playerControl => playerControl.PlayerScoreControl.TotalScore);
        PlayerControl secondPlayerControl = singSceneControl.PlayerControls
            .Except(new List<PlayerControl> { firstPlayerControl })
            .FindMaxElement(playerControl => playerControl.PlayerScoreControl.TotalScore);
        return Math.Abs(firstPlayerControl.PlayerScoreControl.TotalScore - secondPlayerControl.PlayerScoreControl.TotalScore);
    }
}
