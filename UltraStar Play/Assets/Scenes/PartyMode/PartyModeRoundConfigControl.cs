using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeRoundConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSettings partyModeSettings;

    [Inject]
    private GameRoundSettings gameRoundSettings;

    [Inject(UxmlName = R.UxmlNames.roundTitleLabel)]
    private Label roundTitleLabel;

    [Inject(UxmlName = R.UxmlNames.finishConditionPicker)]
    private ItemPicker finishConditionPicker;

    [Inject(UxmlName = R.UxmlNames.finishConditionPointsPicker)]
    private ItemPicker finishConditionPointsPicker;

    [Inject(UxmlName = R.UxmlNames.modifierConditionPicker)]
    private ItemPicker modifierConditionPicker;

    [Inject(UxmlName = R.UxmlNames.modifierConditionFromNumberPicker)]
    private ItemPicker modifierConditionFromNumberPicker;

    [Inject(UxmlName = R.UxmlNames.modifierConditionUntilNumberPicker)]
    private ItemPicker modifierConditionUntilNumberPicker;

    [Inject(UxmlName = R.UxmlNames.deleteRoundButton)]
    public Button DeleteRoundButton { get; private set; }

    private LabeledItemPickerControl<int> modifierConditionFromNumberPickerControl;
    private LabeledItemPickerControl<int> modifierConditionUntilNumberPickerControl;

    public void OnInjectionFinished()
    {
        int roundIndex = partyModeSettings.RoundsSettings.GameRoundSettings.IndexOf(gameRoundSettings);
        roundTitleLabel.text = StringUtils.AddLeadingZeros(roundIndex + 1, 2);

        LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl = new(finishConditionPicker, EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        LabeledItemPickerControl<int> finishConditionPointsPickerControl = new(finishConditionPointsPicker, NumberUtils.CreateIntList(1000, 9000, 1000));

        LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl = new(modifierConditionPicker, EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());
        modifierConditionFromNumberPickerControl = new(modifierConditionFromNumberPicker, new List<int> { 0 });
        modifierConditionUntilNumberPickerControl = new(modifierConditionUntilNumberPicker, new List<int> { 0 });

        // Finish condition
        finishConditionPickerControl.Bind(
            () => gameRoundSettings.FinishConditionSettings.Condition,
            newValue =>
            {
                gameRoundSettings.FinishConditionSettings.Condition = newValue;
                UpdateControls();
            });

        finishConditionPointsPickerControl.Bind(
            () => gameRoundSettings.FinishConditionSettings.Points,
            newValue => gameRoundSettings.FinishConditionSettings.Points = newValue);

        // Modifier condition
        modifierConditionPickerControl.Bind(
            () => gameRoundSettings.ModifierConditionSettings.Condition,
            newValue =>
            {
                gameRoundSettings.ModifierConditionSettings.Condition = newValue;
                UpdateControls();
            });

        // Modifier condition from
        modifierConditionFromNumberPickerControl.Bind(
            () =>
            {
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    return gameRoundSettings.ModifierConditionSettings.ScoreFrom;
                }
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    return gameRoundSettings.ModifierConditionSettings.TimeFrom;
                }

                return 0;
            },
            newValue =>
            {
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    gameRoundSettings.ModifierConditionSettings.ScoreFrom = newValue;
                }
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    gameRoundSettings.ModifierConditionSettings.TimeFrom = newValue;
                }
            });

        // Modifier condition until
        modifierConditionUntilNumberPickerControl.Bind(
            () =>
            {
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    return gameRoundSettings.ModifierConditionSettings.ScoreUntil;
                }
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    return gameRoundSettings.ModifierConditionSettings.TimeUntil;
                }

                return 0;
            },
            newValue =>
            {
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    gameRoundSettings.ModifierConditionSettings.ScoreUntil = newValue;
                }
                if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    gameRoundSettings.ModifierConditionSettings.TimeUntil = newValue;
                }
            });

        UpdateControls();
    }

    private void UpdateControls()
    {
        bool pointsPickerVisible = gameRoundSettings.FinishConditionSettings.Condition != EGameRoundFinishCondition.ReachEndOfSong;
        finishConditionPointsPicker.SetVisibleByDisplay(pointsPickerVisible);

        bool showRangePicker = gameRoundSettings.ModifierConditionSettings.Condition
            is EGameRoundModifierCondition.ScoreRange
            or EGameRoundModifierCondition.TimeRange;
        modifierConditionFromNumberPickerControl.ItemPicker.SetVisibleByDisplay(showRangePicker);
        modifierConditionUntilNumberPickerControl.ItemPicker.SetVisibleByDisplay(showRangePicker);

        List<int> modifierConditionValues = null;
        if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 10000, 1000);
        }
        else if (gameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 100, 10);
        }

        if (!modifierConditionValues.IsNullOrEmpty())
        {
            modifierConditionFromNumberPickerControl.Items = modifierConditionValues;
            modifierConditionFromNumberPickerControl.SelectItem(modifierConditionValues.FirstOrDefault());

            modifierConditionUntilNumberPickerControl.Items = modifierConditionValues;
            modifierConditionUntilNumberPickerControl.SelectItem(modifierConditionValues.LastOrDefault());
        }
    }
}
