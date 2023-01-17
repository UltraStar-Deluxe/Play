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
    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneFinisher singSceneFinisher;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public ReactiveProperty<int> ModifiedVolumePercent { get; private set; } = new(100);

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
        if (isModifierConditionTriggered)
        {
            if (modifiers.Contains(EGameRoundModifier.HideLyrics))
            {
                singSceneControl.HideLyricsByVisibility(playerControl.Voice);
            }
            if (modifiers.Contains(EGameRoundModifier.HideNotes))
            {
                playerControl.PlayerUiControl.HideNotesByVisibility();
            }
            if (modifiers.Contains(EGameRoundModifier.HideScore))
            {
                playerControl.PlayerUiControl.HideScoreByVisibility();
            }
        }
        else
        {
            if (modifiers.Contains(EGameRoundModifier.HideLyrics))
            {
                singSceneControl.ShowLyricsByVisibility(playerControl.Voice);
            }
            if (modifiers.Contains(EGameRoundModifier.HideNotes))
            {
                playerControl.PlayerUiControl.ShowNotesByVisibility();
            }
            if (modifiers.Contains(EGameRoundModifier.HideScore))
            {
                playerControl.PlayerUiControl.ShowScoreByVisibility();
            }
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
