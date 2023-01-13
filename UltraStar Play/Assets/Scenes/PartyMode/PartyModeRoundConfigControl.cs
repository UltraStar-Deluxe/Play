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

    private LabeledItemPickerControl<int> modifierConditionFromNumberPickerControl;
    private LabeledItemPickerControl<int> modifierConditionUntilNumberPickerControl;
    private LabeledItemPickerControl<PartyModeRoundSettingsPreset> presetPickerControl;

    private readonly Subject<GameRoundSettings> deletedEventStream = new();
    public IObservable<GameRoundSettings> DeletedEventStream => deletedEventStream;

    private readonly Subject<GameRoundSettings> foldEventStream = new();
    public IObservable<GameRoundSettings> FoldEventStream => foldEventStream;

    private readonly Subject<GameRoundSettings> unfoldEventStream = new();
    public IObservable<GameRoundSettings> UnfoldEventStream => unfoldEventStream;

    private readonly Subject<PartyModeRoundSettingsPreset> presetsChangedEventStream = new();
    public IObservable<PartyModeRoundSettingsPreset> PresetsChangedEventStream => presetsChangedEventStream;

    private readonly Subject<PartyModeRoundSettingsPreset> appliedPresetEventStream = new();
    public IObservable<PartyModeRoundSettingsPreset> AppliedPresetEventStream => appliedPresetEventStream;

    public void OnInjectionFinished()
    {
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

        presetPickerControl = new(presetItemPicker, GetSelectablePresets());
        presetPickerControl.GetLabelTextFunction = preset => preset == null ? "No preset" : preset.Name;
        presetPickerControl.Selection.Subscribe(preset =>
        {
            Unfold(true);
            ApplyPreset(preset);
            UpdatePresetPickerSaveDeleteButtons();
        });

        // Finish condition
        LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl = new(finishConditionPicker, EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        LabeledItemPickerControl<int> finishConditionPointsPickerControl = new(finishConditionPointsPicker, NumberUtils.CreateIntList(1000, 9000, 1000));

        LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl = new(modifierConditionPicker, EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());
        modifierConditionFromNumberPickerControl = new(modifierConditionFromNumberPicker, new List<int> { 0 });
        modifierConditionUntilNumberPickerControl = new(modifierConditionUntilNumberPicker, new List<int> { 0 });

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

        // Modifiers
        HashSetChipsComboControl<EGameRoundModifier> modifierChipsComboControl = new(modifierChipsCombo, EnumUtils.GetValuesAsList<EGameRoundModifier>());
        modifierChipsComboControl.Bind(
            () => GameRoundSettings.ModifierSettings,
            newValue => GameRoundSettings.ModifierSettings = newValue);

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
        UpdatePresetPicker();
        UpdatePresetPickerSaveDeleteButtons();
    }

    private void UpdatePresetPicker()
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
        GameRoundSettings gameRoundSettingsCopy = GameRoundSettings.Clone();
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

        GameRoundSettings presetGameRoundSettingsCopy = preset.GameRoundSettings.Clone();
        partyModeSettings.RoundsSettings.GameRoundSettings.Replace(GameRoundSettings, presetGameRoundSettingsCopy);

        Debug.Log($"Applied preset '{preset.Name}'");
        appliedPresetEventStream.OnNext(preset);
    }

    private void RandomizeRound()
    {
        // TODO: Implement
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

    private List<PartyModeRoundSettingsPreset> GetSelectablePresets()
    {
        return new List<PartyModeRoundSettingsPreset> { null }
            .Union(settings.PartyModeSettings.RoundSettingsPresets)
            .ToList();
    }
}
