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
using Random = System.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeRoundConfigControl : INeedInjection, IInjectionFinishedListener
{
    private const string FoldedClassName = "folded";

    [Inject(Key = nameof(valueInputDialogUi))]
    private VisualTreeAsset valueInputDialogUi;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.dialogContainer)]
    private VisualElement dialogContainer;

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

    [Inject(UxmlName = R.UxmlNames.modifierChipsCombo)]
    private ChipsCombo modifierChipsCombo;

    [Inject(UxmlName = R.UxmlNames.deleteRoundButton)]
    private Button deleteRoundButton;

    [Inject(UxmlName = R.UxmlNames.toggleRoundExpandedButton)]
    private Button toggleRoundExpandedButton;

    [Inject(UxmlName = R.UxmlNames.randomizeButton)]
    private Button randomizeButton;

    [Inject(UxmlName = R.UxmlNames.savePresetButton)]
    private Button savePresetButton;

    [Inject(UxmlName = R.UxmlNames.deletePresetButton)]
    private Button deletePresetButton;

    [Inject(UxmlName = R.UxmlNames.presetItemPicker)]
    private ItemPicker presetItemPicker;

    private bool IsFolded => visualElement.ClassListContains(FoldedClassName);

    private LabeledItemPickerControl<PartyModeRoundSettingsPreset> presetPickerControl;
    private LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl;
    private LabeledItemPickerControl<int> finishConditionPointsPickerControl;
    private HashSetChipsComboControl<EGameRoundModifier> modifierChipsComboControl;
    private LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl;
    private LabeledItemPickerControl<int> modifierConditionFromNumberPickerControl;
    private LabeledItemPickerControl<int> modifierConditionUntilNumberPickerControl;

    private readonly Subject<GameRoundSettings> deletedEventStream = new();
    public IObservable<GameRoundSettings> DeletedEventStream => deletedEventStream;

    private readonly Subject<GameRoundSettings> foldEventStream = new();
    public IObservable<GameRoundSettings> FoldEventStream => foldEventStream;

    private readonly Subject<GameRoundSettings> unfoldEventStream = new();
    public IObservable<GameRoundSettings> UnfoldEventStream => unfoldEventStream;

    private readonly Subject<PartyModeRoundSettingsPreset> presetsChangedEventStream = new();
    public IObservable<PartyModeRoundSettingsPreset> PresetsChangedEventStream => presetsChangedEventStream;

    public void OnInjectionFinished()
    {
        CreateControlObjects();

        // Title label
        int roundIndex = partyModeSettings.RoundsSettings.GameRoundSettings.IndexOf(GameRoundSettings);
        roundTitleLabel.text = StringUtils.AddLeadingZeros(roundIndex + 1, 2);

        // Top buttons
        toggleRoundExpandedButton.RegisterCallbackButtonTriggered(() => ToggleFold());
        deleteRoundButton.RegisterCallbackButtonTriggered(() => DeleteRound());
        deleteRoundButton.SetEnabled(partyModeSettings.RoundsSettings.GameRoundSettings.Count > 1);
        randomizeButton.RegisterCallbackButtonTriggered(() => RandomizeRound());

        // Presets
        savePresetButton.RegisterCallbackButtonTriggered(() => OpenSavePresetDialog());
        deletePresetButton.RegisterCallbackButtonTriggered(() => DeleteSelectedPreset());
        presetPickerControl.Selection.Subscribe(preset =>
        {
            Unfold(true);
            ApplyPreset(preset);
            UpdatePresetPickerSaveDeleteButtons();
        });

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
            newValue =>
            {
                GameRoundSettings.FinishConditionSettings.Points = newValue;
                UpdateControls();
            });

        // Modifiers
        modifierChipsComboControl.Bind(
            () => GameRoundSettings.Modifiers,
            newValue =>
            {
                GameRoundSettings.Modifiers = newValue;
                UpdateControls();
            });

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
                UpdateControls();
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
                if (GameRoundSettings.ModifierConditionSettings.Condition
                    is EGameRoundModifierCondition.ScoreRange
                    or EGameRoundModifierCondition.PlayerAdvance)
                {
                    GameRoundSettings.ModifierConditionSettings.ScoreUntil = newValue;
                    UpdateControls();
                }
                else if (GameRoundSettings.ModifierConditionSettings.Condition is EGameRoundModifierCondition.TimeRange)
                {
                    GameRoundSettings.ModifierConditionSettings.TimeUntil = newValue;
                    UpdateControls();
                }
            });

        UpdateControls();
    }

    private void CreateControlObjects()
    {
        // Presets
        presetPickerControl = new(presetItemPicker, GetSelectablePresets());
        presetPickerControl.GetLabelTextFunction = preset => preset == null ? "No preset" : preset.Name;

        // Finish condition
        finishConditionPickerControl = new(finishConditionPicker, EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        finishConditionPointsPickerControl = new(finishConditionPointsPicker, NumberUtils.CreateIntList(1000, 9000, 1000));

        // Modifiers
        modifierChipsComboControl = new(modifierChipsCombo, EnumUtils.GetValuesAsList<EGameRoundModifier>());
        modifierConditionPickerControl = new(modifierConditionPicker, EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());
        modifierConditionFromNumberPickerControl = new(modifierConditionFromNumberPicker, new List<int> { 0 });
        modifierConditionUntilNumberPickerControl = new(modifierConditionUntilNumberPicker, new List<int> { 0 });
    }

    public void UpdatePresetPicker()
    {
        presetPickerControl.Items = GetSelectablePresets();
        presetPickerControl.SelectItem(GetMatchingPreset());
    }

    private void UpdatePresetPickerSaveDeleteButtons()
    {
        savePresetButton.SetVisibleByDisplay(presetPickerControl.SelectedItem == null);
        deletePresetButton.SetVisibleByDisplay(presetPickerControl.SelectedItem != null);
    }

    private PartyModeRoundSettingsPreset GetMatchingPreset()
    {
        PartyModeRoundSettingsPreset matchingPreset = partyModeSettings.RoundSettingsPresets.FirstOrDefault(preset =>
            preset.GameRoundSettings.EqualsOther(GameRoundSettings));
        return matchingPreset;
    }

    private void OpenSavePresetDialog()
    {
        VisualElement dialogVisualElement = valueInputDialogUi.CloneTreeAndGetFirstChild();
        dialogContainer.Add(dialogVisualElement);
        dialogVisualElement.AddToClassList("overlay");

        TextInputDialogControl dialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<TextInputDialogControl>();
        dialogControl.Title = "Save preset";
        dialogControl.Message = "Enter preset name";

        dialogControl.SubmitValueEventStream
            .Subscribe(presetName => SavePreset(presetName));
    }

    private void SavePreset(string presetName)
    {
        GameRoundSettings gameRoundSettingsCopy = new();
        gameRoundSettingsCopy.CopyValues(GameRoundSettings);
        PartyModeRoundSettingsPreset newPreset = new(presetName, gameRoundSettingsCopy);

        PartyModeRoundSettingsPreset existingPreset = partyModeSettings.RoundSettingsPresets
            .FirstOrDefault(preset => string.Equals(preset.Name, presetName, StringComparison.InvariantCultureIgnoreCase));
        if (existingPreset != null)
        {
            partyModeSettings.RoundSettingsPresets.Replace(existingPreset, newPreset);
        }
        else
        {
            partyModeSettings.RoundSettingsPresets.Add(newPreset);
        }

        Debug.Log($"Added or updated game round preset '{presetName}'");
        presetsChangedEventStream.OnNext(newPreset);
    }

    private void DeleteSelectedPreset()
    {
        PartyModeRoundSettingsPreset preset = presetPickerControl.SelectedItem;
        if (preset == null)
        {
            return;
        }

        partyModeSettings.RoundSettingsPresets.Remove(preset);

        Debug.Log($"Deleted game round preset '{preset.Name}'");
        presetsChangedEventStream.OnNext(preset);
    }

    private void ApplyPreset(PartyModeRoundSettingsPreset preset)
    {
        if (preset == null
            || preset.GameRoundSettings.EqualsOther(GameRoundSettings))
        {
            return;
        }

        GameRoundSettings.CopyValues(preset.GameRoundSettings);
        UpdateControls();
    }

    private void RandomizeRound()
    {
        GameRoundSettings.FinishConditionSettings.Condition = RandomUtils.RandomOf(EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        GameRoundSettings.FinishConditionSettings.Points = RandomUtils.RandomOf(NumberUtils.CreateIntList(1000, 9000, 1000));

        GameRoundSettings.Modifiers = RandomUtils.RandomHashSetOf(EnumUtils.GetValuesAsList<EGameRoundModifier>());
        GameRoundSettings.ModifierConditionSettings.Condition = RandomUtils.RandomOf(EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());

        GameRoundSettings.ModifierConditionSettings.ScoreFrom = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 10000, 1000));
        GameRoundSettings.ModifierConditionSettings.ScoreUntil = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 10000, 1000));
        if (GameRoundSettings.ModifierConditionSettings.ScoreFrom > GameRoundSettings.ModifierConditionSettings.ScoreUntil)
        {
            // Swap values via deconstruction
            (GameRoundSettings.ModifierConditionSettings.ScoreFrom, GameRoundSettings.ModifierConditionSettings.ScoreUntil) = (GameRoundSettings.ModifierConditionSettings.ScoreUntil, GameRoundSettings.ModifierConditionSettings.ScoreFrom);
        }

        GameRoundSettings.ModifierConditionSettings.TimeFrom = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 100, 10));
        GameRoundSettings.ModifierConditionSettings.TimeUntil = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 100, 10));
        if (GameRoundSettings.ModifierConditionSettings.TimeFrom > GameRoundSettings.ModifierConditionSettings.TimeUntil)
        {
            // Swap values via deconstruction
            (GameRoundSettings.ModifierConditionSettings.TimeFrom, GameRoundSettings.ModifierConditionSettings.TimeUntil) = (GameRoundSettings.ModifierConditionSettings.TimeUntil, GameRoundSettings.ModifierConditionSettings.TimeFrom);
        }

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

        visualElement.AddToClassList(FoldedClassName);

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

        visualElement.RemoveFromClassList(FoldedClassName);

        if (notify)
        {
            unfoldEventStream.OnNext(GameRoundSettings);
        }
    }

    public void UpdateControls()
    {
        // Finish condition
        finishConditionPickerControl.SelectItem(GameRoundSettings.FinishConditionSettings.Condition);
        finishConditionPointsPickerControl.SelectItem(GameRoundSettings.FinishConditionSettings.Points);
        finishConditionPointsPickerControl.ItemPicker.SetVisibleByDisplay(GameRoundSettings.FinishConditionSettings.Condition != EGameRoundFinishCondition.ReachEndOfSong);

        // Modifiers
        modifierChipsComboControl.SelectItem(GameRoundSettings.Modifiers);

        // Modifier condition
        modifierConditionPickerControl.ItemPicker.SetVisibleByDisplay(!GameRoundSettings.Modifiers.IsNullOrEmpty());
        modifierConditionPickerControl.SelectItem(GameRoundSettings.ModifierConditionSettings.Condition);

        // Modifier condition from/until
        bool modifierConditionNumberPickersVisible = modifierConditionPickerControl.ItemPicker.IsVisibleByDisplay()
            && GameRoundSettings.ModifierConditionSettings.Condition
                is EGameRoundModifierCondition.ScoreRange
                or EGameRoundModifierCondition.TimeRange;
        modifierConditionFromNumberPickerControl.ItemPicker.SetVisibleByDisplay(modifierConditionNumberPickersVisible
            || (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.PlayerAdvance
                && modifierConditionPickerControl.ItemPicker.IsVisibleByDisplay()));
        modifierConditionUntilNumberPickerControl.ItemPicker.SetVisibleByDisplay(modifierConditionNumberPickersVisible);

        List<int> modifierConditionValues = new();
        if (GameRoundSettings.ModifierConditionSettings.Condition == EGameRoundModifierCondition.TimeRange)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 100, 10);
            modifierConditionFromNumberPickerControl.SelectItem(GameRoundSettings.ModifierConditionSettings.TimeFrom);
            modifierConditionUntilNumberPickerControl.SelectItem(GameRoundSettings.ModifierConditionSettings.TimeUntil);
            modifierConditionFromNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue} %";
            modifierConditionUntilNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue} %";
        }
        else if (GameRoundSettings.ModifierConditionSettings.Condition
            is EGameRoundModifierCondition.ScoreRange
            or EGameRoundModifierCondition.PlayerAdvance)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 10000, 1000);
            modifierConditionFromNumberPickerControl.SelectItem(GameRoundSettings.ModifierConditionSettings.ScoreFrom);
            modifierConditionUntilNumberPickerControl.SelectItem(GameRoundSettings.ModifierConditionSettings.ScoreUntil);
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

        UpdatePresetPicker();
        UpdatePresetPickerSaveDeleteButtons();
    }

    private List<PartyModeRoundSettingsPreset> GetSelectablePresets()
    {
        return new List<PartyModeRoundSettingsPreset> { null }
            .Union(settings.PartyModeSettings.RoundSettingsPresets)
            .ToList();
    }
}
