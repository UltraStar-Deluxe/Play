using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class SingModifier
{
    [Flags]
    public enum EModifierAction
    {
        HideNotes = 1 << 0,
        HideScore = 1 << 1,
        HideLyrics = 1 << 2,
        MuteAudio = 1 << 3,
    }

    public struct ModifierTrigger
    {
        public enum ETriggerType
        {
            Infinite,
            AfterTime,
            UntilTime,
            TimeRange,
            AfterScore,
            UntilScore,
            ScoreRange
        }

        public ETriggerType triggerType;
        public float timeMin;
        public float timeMax;
        public float scoreMin;
        public float scoreMax;

        internal static ModifierTrigger Default()
        {
            return new ModifierTrigger()
            {
                triggerType = ETriggerType.Infinite,
                timeMin = 0.3f,
                timeMax = 0.6f,
                scoreMin = 2000,
                scoreMax = 8000,
            };
        }
    }

    public EModifierAction modifierActions;
    public ModifierTrigger trigger;
    public bool applyToPlayersIndividually;

    public Action<EModifierAction, int> onApply;
    public Action<EModifierAction, int> onDisable;

    bool applied;
    bool[] appliedTo = new bool[10]; // TODO should reflect the max number of concurrent singers
    bool disabled;

    bool ApplyToPlayersIndividually => applyToPlayersIndividually
                                       && modifierActions
                                           is EModifierAction.HideNotes
                                           or EModifierAction.HideScore
                                       && trigger.triggerType
                                           is ModifierTrigger.ETriggerType.AfterScore
                                           or ModifierTrigger.ETriggerType.UntilScore
                                           or ModifierTrigger.ETriggerType.ScoreRange;

    public static implicit operator SingModifier(EModifierAction action) => new SingModifier(action);

    public SingModifier()
    {
        this.modifierActions = 0;
        this.trigger = ModifierTrigger.Default();
    }

    public SingModifier(EModifierAction modifierActions)
    {
        this.modifierActions = modifierActions;
        this.trigger = ModifierTrigger.Default();
    }

    public void UpdateModifier(double songPositionMs, double songDurationMs, float[] playerScores)
    {
        if (modifierActions == 0 || (this.disabled && !ApplyToPlayersIndividually))
        {
            return;
        }

        // For individual players:
        if (ApplyToPlayersIndividually)
        {
            for (int playerIndex = 0; playerIndex < playerScores.Length; playerIndex++)
            {
                float playerScore = playerScores[playerIndex];
                switch (trigger.triggerType)
                {
                    case ModifierTrigger.ETriggerType.AfterScore:
                    {
                        if (appliedTo[playerIndex])
                            return;

                        if (playerScore >= trigger.scoreMin)
                            this.ApplyTo(playerIndex);

                        break;
                    }
                    case ModifierTrigger.ETriggerType.UntilScore:
                    {
                        if (!this.applied && !this.disabled)
                            this.ApplyTo(playerIndex);
                        else if (playerScore >= trigger.scoreMin)
                            this.DisableFor(playerIndex);

                        break;
                    }
                    case ModifierTrigger.ETriggerType.ScoreRange:
                    {
                        if (!this.applied)
                        {
                            if (playerScore >= trigger.scoreMin)
                                this.ApplyTo(playerIndex);
                        }
                        else
                        {
                            if (playerScore >= trigger.scoreMax)
                                this.DisableFor(playerIndex);
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // For all players:
        float maxPlayerScore = playerScores.Max();
        double songPositionNormalized = songPositionMs / songDurationMs;

        switch (trigger.triggerType)
        {
            case ModifierTrigger.ETriggerType.Infinite:
            {
                if (this.applied)
                {
                    return;
                }

                this.Apply();
                break;
            }
            case ModifierTrigger.ETriggerType.TimeRange:
            {
                if (!this.applied)
                {
                    if (songPositionNormalized >= trigger.timeMin)
                        this.Apply();
                }
                else
                {
                    if (songPositionNormalized >= trigger.timeMax)
                        this.Disable();
                }

                break;
            }
            case ModifierTrigger.ETriggerType.AfterTime:
            {
                if (this.applied)
                    return;

                if (songPositionNormalized >= trigger.timeMin)
                    this.Apply();

                break;
            }
            case ModifierTrigger.ETriggerType.UntilTime:
            {
                if (!this.applied && !this.disabled)
                    this.Apply();
                else if (songPositionNormalized >= trigger.timeMin)
                    this.Disable();

                break;
            }
            case ModifierTrigger.ETriggerType.AfterScore:
            {
                if (this.applied)
                    return;

                if (maxPlayerScore >= trigger.scoreMin)
                    this.Apply();

                break;
            }
            case ModifierTrigger.ETriggerType.UntilScore:
            {
                if (!this.applied && !this.disabled)
                    this.Apply();
                else if (maxPlayerScore >= trigger.scoreMin)
                    this.Disable();

                break;
            }
            case ModifierTrigger.ETriggerType.ScoreRange:
            {
                if (!this.applied)
                {
                    if (maxPlayerScore >= trigger.scoreMin)
                        this.Apply();
                }
                else
                {
                    if (maxPlayerScore >= trigger.scoreMax)
                        this.Disable();
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void Apply() { ApplyTo(-1); }

    void ApplyTo(int player)
    {
        Debug.Log("<color=#ffff00>+ APPLY MODIFIER</color>");
        if (player >= 0)
            this.appliedTo[player] = true;
        else
            this.applied = true;
        onApply?.Invoke(this.modifierActions, player);
    }

    void Disable() { DisableFor(-1); }

    void DisableFor(int player)
    {
        Debug.Log($"<color=#ff8000>- DISABLE MODIFIER</color>");
        if (player >= 0)
            this.appliedTo[player] = false;
        else
        {
            this.applied = false;
            this.disabled = true;
        }

        onDisable?.Invoke(this.modifierActions, player);
    }
}
