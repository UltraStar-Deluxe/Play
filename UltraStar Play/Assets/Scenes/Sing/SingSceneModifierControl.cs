using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneModifierControl : INeedInjection, IInjectionFinishedListener
{
    private const float AnimTimeInSeconds = 1.5f;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneFinisher singSceneFinisher;

    [Inject]
    private SongAudioPlayer songAudioPlayer;
    
    [Inject]
    private SingSceneData sceneData;

    [Inject]
    private Injector injector;
    
    [Inject]
    private Settings settings;

    private readonly SingScenePassTheMicControl passTheMicControl = new();

    public ReactiveProperty<int> ModifiedVolumePercent { get; private set; } = new(100);

    private readonly Dictionary<PlayerControl, HashSet<EGameRoundModifier>> playerControlToActiveModifiers = new();
    private readonly HashSet<EGameRoundModifier> playerIndependentActiveModifiers = new();

    public void OnInjectionFinished()
    {
        injector.Inject(passTheMicControl);

        if (singSceneControl.HasPartyModeSceneData)
        {
            // Check for finish when any score changes after a sentence is complete
            singSceneControl.PlayerControls
                .Select(playerControl => playerControl.PlayerScoreControl.SentenceScoreEventStream)
                .Merge()
                .Subscribe(_ => UpdateFinishCondition());
        }
    }

    public void Update()
    {
        UpdateModifiers();
    }

    private void UpdateFinishCondition()
    {
        if (!singSceneFinisher.IsSongFinished
            && IsFinishConditionTriggered())
        {
            Debug.Log($"Trigger party mode song finish");
            singSceneFinisher.TriggerEarlySongFinish();
        }
    }

    private bool IsFinishConditionTriggered()
    {
        EGameRoundFinishCondition finishCondition = sceneData.gameRoundSettings.finishConditionSettings.condition;
        int finishConditionScore = sceneData.gameRoundSettings.finishConditionSettings.points;
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
        HashSet<EGameRoundModifier> modifiers = sceneData.gameRoundSettings.modifiers;
        bool isModifierConditionTriggered = singSceneControl.PlayerControls
            .AnyMatch(playerControl => IsModifierConditionTriggered(playerControl));
        modifiers.ForEach(modifier =>
        {
            if (isModifierConditionTriggered)
            {
                if (!playerIndependentActiveModifiers.Contains(modifier))
                {
                    playerIndependentActiveModifiers.Add(modifier);
                    ActivatePlayerIndependentModifier(modifier);
                }
            }
            else
            {
                if (playerIndependentActiveModifiers.Contains(modifier))
                {
                    playerIndependentActiveModifiers.Remove(modifier);
                    DeactivatePlayerIndependentModifier(modifier);
                }
            }
        });

        if (!singSceneControl.IsPaused)
        {
            passTheMicControl.Update(Time.deltaTime);
        }
    }

    private void ActivatePlayerIndependentModifier(EGameRoundModifier modifier)
    {
        if (modifier == EGameRoundModifier.ReduceAudio)
        {
            ReduceAudio();
        }
    }

    private void DeactivatePlayerIndependentModifier(EGameRoundModifier modifier)
    {
        if (modifier == EGameRoundModifier.ReduceAudio)
        {
            NormalizeAudio();
        }
    }

    private void NormalizeAudio()
    {
        ModifiedVolumePercent.Value = 100;
    }

    private void ReduceAudio()
    {
        ModifiedVolumePercent.Value = settings.ReducedAudioVolumePercent;
    }

    private void UpdatePlayerSpecificModifiers(PlayerControl playerControl)
    {
        if (!playerControlToActiveModifiers.TryGetValue(playerControl, out HashSet<EGameRoundModifier> activeModifiers))
        {
            activeModifiers = new HashSet<EGameRoundModifier>();
            playerControlToActiveModifiers.Add(playerControl, activeModifiers);
        }

        HashSet<EGameRoundModifier> modifiers = sceneData.gameRoundSettings.modifiers;
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
            playerControl.PlayerUiControl.NoteDisplayer.FadeOutLyricsOnNotes(AnimTimeInSeconds);
        }
        else if (modifier == EGameRoundModifier.HideNotes)
        {
            playerControl.PlayerUiControl.FadeOutNotes(AnimTimeInSeconds);
        }
    }

    private void DeactivateModifier(PlayerControl playerControl, EGameRoundModifier modifier)
    {
        if (modifier == EGameRoundModifier.HideLyrics)
        {
            singSceneControl.FadeInLyrics(playerControl.Voice, AnimTimeInSeconds);
            playerControl.PlayerUiControl.NoteDisplayer.FadeInLyricsOnNotes(AnimTimeInSeconds);
        }
        else if (modifier == EGameRoundModifier.HideNotes)
        {
            playerControl.PlayerUiControl.FadeInNotes(AnimTimeInSeconds);
        }
    }

    private bool IsModifierConditionTriggered(PlayerControl playerControl)
    {
        GameRoundModifierConditionSettings modifierConditionSettings = sceneData.gameRoundSettings.modifierConditionSettings;
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
            // Only the first player should be affected when the condition is "player advance"
            PlayerControl firstPlayerControl = GetFirstPlayerControl();
            if (playerControl == firstPlayerControl)
            {
                return GetFirstPlayerScoreDistanceToSecondPlayer() >= modifierConditionSettings.scoreFrom;
            }
        }

        return false;
    }

    private int GetFirstPlayerScoreDistanceToSecondPlayer()
    {
        PlayerControl firstPlayerControl = GetFirstPlayerControl();
        if (firstPlayerControl == null)
        {
            return 0;
        }

        PlayerControl secondPlayerControl = GetSecondPlayerControl(firstPlayerControl);
        if (secondPlayerControl == null)
        {
            return 0;
        }

        return Math.Abs(firstPlayerControl.PlayerScoreControl.TotalScore - secondPlayerControl.PlayerScoreControl.TotalScore);
    }

    private PlayerControl GetFirstPlayerControl()
    {
        PlayerControl firstPlayerControl = singSceneControl.PlayerControls
            .FindMaxElement(playerControl => playerControl.PlayerScoreControl.TotalScore);
        return firstPlayerControl;
    }

    private PlayerControl GetSecondPlayerControl(PlayerControl firstPlayerControl)
    {
        PlayerControl secondPlayerControl = singSceneControl.PlayerControls
            .Except(new List<PlayerControl> { firstPlayerControl })
            .FindMaxElement(playerControl => playerControl.PlayerScoreControl.TotalScore);
        return secondPlayerControl;
    }
}
