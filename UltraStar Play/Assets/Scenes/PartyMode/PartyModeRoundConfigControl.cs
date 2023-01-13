using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using Unity.Android.Types;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeRoundConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSettings partyModeSettings;

    [Inject]
    public GameRoundSettings GameRoundSettings { get; private set; }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

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
    private Button deleteRoundButton;

    [Inject(UxmlName = R.UxmlNames.toggleRoundExpandedButton)]
    private Button toggleRoundExpandedButton;

    private bool IsFolded => visualElement.ClassListContains("folded");

    private LabeledItemPickerControl<int> modifierConditionFromNumberPickerControl;
    private LabeledItemPickerControl<int> modifierConditionUntilNumberPickerControl;

    private Subject<GameRoundSettings> deletedEventStream = new();
    public IObservable<GameRoundSettings> DeletedEventStream => deletedEventStream;

    private Subject<GameRoundSettings> foldEventStream = new();
    public IObservable<GameRoundSettings> FoldEventStream => foldEventStream;

    private Subject<GameRoundSettings> unfoldEventStream = new();
    public IObservable<GameRoundSettings> UnfoldEventStream => unfoldEventStream;

    public void OnInjectionFinished()
    {
        int roundIndex = partyModeSettings.RoundsSettings.GameRoundSettings.IndexOf(GameRoundSettings);
        roundTitleLabel.text = StringUtils.AddLeadingZeros(roundIndex + 1, 2);

        toggleRoundExpandedButton.RegisterCallbackButtonTriggered(() => ToggleFold());

        LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl = new(finishConditionPicker, EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        LabeledItemPickerControl<int> finishConditionPointsPickerControl = new(finishConditionPointsPicker, NumberUtils.CreateIntList(1000, 9000, 1000));

        LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl = new(modifierConditionPicker, EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());
        modifierConditionFromNumberPickerControl = new(modifierConditionFromNumberPicker, new List<int> { 0 });
        modifierConditionUntilNumberPickerControl = new(modifierConditionUntilNumberPicker, new List<int> { 0 });

        // Delete button
        deleteRoundButton.RegisterCallbackButtonTriggered(() => DeleteRound());
        deleteRoundButton.SetEnabled(partyModeSettings.RoundsSettings.GameRoundSettings.Count > 1);

        // Finish condition
        finishConditionPickerControl.Bind(
            () => GameRoundSettings.FinishConditionSettings.Condition,
            newValue =>
            {
                GameRoundSettings.FinishConditionSettings.Condition = newValue;
                UpdateControls();
            });

        finishConditionPointsPickerControl.Bind(
            () => GameRoundSettings.FinishConditionSettings.Points,
            newValue => GameRoundSettings.FinishConditionSettings.Points = newValue);

        // Modifier condition
        modifierConditionPickerControl.Bind(
            () => GameRoundSettings.ModifierConditionSettings.Condition,
            newValue =>
            {
                GameRoundSettings.ModifierConditionSettings.Condition = newValue;
                UpdateControls();
            });

        // Modifier condition from
        modifierConditionFromNumberPickerControl.Bind(
            () =>
            {
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    return GameRoundSettings.ModifierConditionSettings.ScoreFrom;
                }
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    return GameRoundSettings.ModifierConditionSettings.TimeFrom;
                }

                return 0;
            },
            newValue =>
            {
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    GameRoundSettings.ModifierConditionSettings.ScoreFrom = newValue;
                }
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    GameRoundSettings.ModifierConditionSettings.TimeFrom = newValue;
                }
            });

        // Modifier condition until
        modifierConditionUntilNumberPickerControl.Bind(
            () =>
            {
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    return GameRoundSettings.ModifierConditionSettings.ScoreUntil;
                }
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    return GameRoundSettings.ModifierConditionSettings.TimeUntil;
                }

                return 0;
            },
            newValue =>
            {
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.ScoreRange)
                {
                    GameRoundSettings.ModifierConditionSettings.ScoreUntil = newValue;
                }
                if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
                {
                    GameRoundSettings.ModifierConditionSettings.TimeUntil = newValue;
                }
            });

        UpdateControls();
    }

    private void DeleteRound()
    {
        visualElement.RemoveFromHierarchy();
        deletedEventStream.OnNext(GameRoundSettings);
    }

    private void ToggleFold()
    {
        if (IsFolded)
        {
            Unfold(true);
        }
        else
        {
            Fold(true);
        }
    }

    public void Fold(bool notify)
    {
        if (IsFolded)
        {
            return;
        }

        visualElement.AddToClassList("folded");

        if (notify)
        {
            foldEventStream.OnNext(GameRoundSettings);
        }
    }

    public void Unfold(bool notify)
    {
        if (!IsFolded)
        {
            return;
        }

        visualElement.RemoveFromClassList("folded");

        if (notify)
        {
            unfoldEventStream.OnNext(GameRoundSettings);
        }
    }

    private void UpdateControls()
    {
        bool finishConditionPointsPickerVisible = GameRoundSettings.FinishConditionSettings.Condition != EGameRoundFinishCondition.ReachEndOfSong;
        finishConditionPointsPicker.SetVisibleByDisplay(finishConditionPointsPickerVisible);

        bool modifierConditionPickersVisible = GameRoundSettings.ModifierConditionSettings.Condition
            is EGameRoundModifierCondition.ScoreRange
            or EGameRoundModifierCondition.TimeRange;
        modifierConditionFromNumberPickerControl.ItemPicker.SetVisibleByDisplay(modifierConditionPickersVisible
            || GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.PlayerAdvance);
        modifierConditionUntilNumberPickerControl.ItemPicker.SetVisibleByDisplay(modifierConditionPickersVisible);

        List<int> modifierConditionValues;
        if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 100, 10);
            modifierConditionFromNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue} %";
            modifierConditionUntilNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue} %";
        }
        else
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 10000, 1000);
            modifierConditionFromNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue}";
            modifierConditionUntilNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue}";
        }

        if (!modifierConditionValues.IsNullOrEmpty())
        {
            modifierConditionFromNumberPickerControl.Items = modifierConditionValues;
            if (!modifierConditionValues.Contains(modifierConditionFromNumberPickerControl.SelectedItem))
            {
                modifierConditionFromNumberPickerControl.SelectItem(modifierConditionValues.FirstOrDefault());
            }

            modifierConditionUntilNumberPickerControl.Items = modifierConditionValues;
            if (!modifierConditionValues.Contains(modifierConditionUntilNumberPickerControl.SelectedItem)
                || modifierConditionUntilNumberPickerControl.SelectedItem == modifierConditionFromNumberPickerControl.SelectedItem)
            {
                modifierConditionUntilNumberPickerControl.SelectItem(modifierConditionValues.LastOrDefault());
            }
        }
    }
}
