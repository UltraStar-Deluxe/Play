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
        int roundIndex = partyModeSettings.roundsSettings.gameRoundSettings.IndexOf(GameRoundSettings);
        roundTitleLabel.text = StringUtils.AddLeadingZeros(roundIndex + 1, 2);

        // Top buttons
        toggleRoundExpandedButton.RegisterCallbackButtonTriggered(() => ToggleFold());
        deleteRoundButton.RegisterCallbackButtonTriggered(() => DeleteRound());
        deleteRoundButton.SetEnabled(partyModeSettings.roundsSettings.gameRoundSettings.Count > 1);
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
            () => GameRoundSettings.finishConditionSettings.condition,
            newValue =>
            {
                GameRoundSettings.finishConditionSettings.condition = newValue;
                UpdateControls();
            });

        finishConditionPointsPickerControl.Bind(
            () => GameRoundSettings.finishConditionSettings.points,
            newValue =>
            {
                GameRoundSettings.finishConditionSettings.points = newValue;
                UpdateControls();
            });

        // Modifiers
        modifierChipsComboControl.Bind(
            () => GameRoundSettings.modifiers,
            newValue =>
            {
                GameRoundSettings.modifiers = newValue;
                UpdateControls();
            });

        // Modifier condition
        modifierConditionPickerControl.Bind(
            () => GameRoundSettings.modifierConditionSettings.condition,
            newValue =>
            {
                GameRoundSettings.modifierConditionSettings.condition = newValue;
                UpdateControls();
            });

        // Modifier condition from
        modifierConditionFromNumberPickerControl.Bind(
            () =>
            {
                if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.ScoreRange)
                {
                    return GameRoundSettings.modifierConditionSettings.scoreFrom;
                }
                if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
                {
                    return GameRoundSettings.modifierConditionSettings.timeFrom;
                }

                return 0;
            },
            newValue =>
            {
                if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.ScoreRange)
                {
                    GameRoundSettings.modifierConditionSettings.scoreFrom = newValue;
                }
                if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
                {
                    GameRoundSettings.modifierConditionSettings.timeFrom = newValue;
                }
                UpdateControls();
            });

        // Modifier condition until
        modifierConditionUntilNumberPickerControl.Bind(
            () =>
            {
                if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.ScoreRange)
                {
                    return GameRoundSettings.modifierConditionSettings.scoreUntil;
                }
                if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
                {
                    return GameRoundSettings.modifierConditionSettings.timeUntil;
                }

                return 0;
            },
            newValue =>
            {
                if (GameRoundSettings.modifierConditionSettings.condition
                    is EGameRoundModifierCondition.ScoreRange
                    or EGameRoundModifierCondition.PlayerAdvance)
                {
                    GameRoundSettings.modifierConditionSettings.scoreUntil = newValue;
                    UpdateControls();
                }
                else if (GameRoundSettings.modifierConditionSettings.condition is EGameRoundModifierCondition.TimeRange)
                {
                    GameRoundSettings.modifierConditionSettings.timeUntil = newValue;
                    UpdateControls();
                }
            });

        UpdateControls();
    }

    private void CreateControlObjects()
    {
        // Presets
        presetPickerControl = new(presetItemPicker, GetSelectablePresets());
        presetPickerControl.GetLabelTextFunction = preset => preset == null ? "No preset" : preset.name;

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
        PartyModeRoundSettingsPreset matchingPreset = partyModeSettings.roundSettingsPresets.FirstOrDefault(preset =>
            preset.gameRoundSettings.EqualsOther(GameRoundSettings));
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

        PartyModeRoundSettingsPreset existingPreset = partyModeSettings.roundSettingsPresets
            .FirstOrDefault(preset => string.Equals(preset.name, presetName, StringComparison.InvariantCultureIgnoreCase));
        if (existingPreset != null)
        {
            partyModeSettings.roundSettingsPresets.Replace(existingPreset, newPreset);
        }
        else
        {
            partyModeSettings.roundSettingsPresets.Add(newPreset);
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

        partyModeSettings.roundSettingsPresets.Remove(preset);

        Debug.Log($"Deleted game round preset '{preset.name}'");
        presetsChangedEventStream.OnNext(preset);
    }

    private void ApplyPreset(PartyModeRoundSettingsPreset preset)
    {
        if (preset == null
            || preset.gameRoundSettings.EqualsOther(GameRoundSettings))
        {
            return;
        }

        GameRoundSettings.CopyValues(preset.gameRoundSettings);
        UpdateControls();
    }

    private void RandomizeRound()
    {
        GameRoundSettings.finishConditionSettings.condition = RandomUtils.RandomOf(EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        GameRoundSettings.finishConditionSettings.points = RandomUtils.RandomOf(NumberUtils.CreateIntList(1000, 9000, 1000));

        GameRoundSettings.modifiers = RandomUtils.RandomHashSetOf(EnumUtils.GetValuesAsList<EGameRoundModifier>());
        GameRoundSettings.modifierConditionSettings.condition = RandomUtils.RandomOf(EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());

        GameRoundSettings.modifierConditionSettings.scoreFrom = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 10000, 1000));
        GameRoundSettings.modifierConditionSettings.scoreUntil = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 10000, 1000));
        if (GameRoundSettings.modifierConditionSettings.scoreFrom > GameRoundSettings.modifierConditionSettings.scoreUntil)
        {
            // Swap values via deconstruction
            (GameRoundSettings.modifierConditionSettings.scoreFrom, GameRoundSettings.modifierConditionSettings.scoreUntil) = (GameRoundSettings.modifierConditionSettings.scoreUntil, GameRoundSettings.modifierConditionSettings.scoreFrom);
        }

        GameRoundSettings.modifierConditionSettings.timeFrom = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 100, 10));
        GameRoundSettings.modifierConditionSettings.timeUntil = RandomUtils.RandomOf(NumberUtils.CreateIntList(0, 100, 10));
        if (GameRoundSettings.modifierConditionSettings.timeFrom > GameRoundSettings.modifierConditionSettings.timeUntil)
        {
            // Swap values via deconstruction
            (GameRoundSettings.modifierConditionSettings.timeFrom, GameRoundSettings.modifierConditionSettings.timeUntil) = (GameRoundSettings.modifierConditionSettings.timeUntil, GameRoundSettings.modifierConditionSettings.timeFrom);
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
        finishConditionPickerControl.SelectItem(GameRoundSettings.finishConditionSettings.condition);
        finishConditionPointsPickerControl.SelectItem(GameRoundSettings.finishConditionSettings.points);
        finishConditionPointsPickerControl.ItemPicker.SetVisibleByDisplay(GameRoundSettings.finishConditionSettings.condition != EGameRoundFinishCondition.ReachEndOfSong);

        // Modifiers
        modifierChipsComboControl.SelectItem(GameRoundSettings.modifiers);

        // Modifier condition
        modifierConditionPickerControl.ItemPicker.SetVisibleByDisplay(!GameRoundSettings.modifiers.IsNullOrEmpty());
        modifierConditionPickerControl.SelectItem(GameRoundSettings.modifierConditionSettings.condition);

        // Modifier condition from/until
        bool modifierConditionNumberPickersVisible = modifierConditionPickerControl.ItemPicker.IsVisibleByDisplay()
            && GameRoundSettings.modifierConditionSettings.condition
                is EGameRoundModifierCondition.ScoreRange
                or EGameRoundModifierCondition.TimeRange;
        modifierConditionFromNumberPickerControl.ItemPicker.SetVisibleByDisplay(modifierConditionNumberPickersVisible
            || (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.PlayerAdvance
                && modifierConditionPickerControl.ItemPicker.IsVisibleByDisplay()));
        modifierConditionUntilNumberPickerControl.ItemPicker.SetVisibleByDisplay(modifierConditionNumberPickersVisible);

        List<int> modifierConditionValues = new();
        if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 100, 10);
            modifierConditionFromNumberPickerControl.SelectItem(GameRoundSettings.modifierConditionSettings.timeFrom);
            modifierConditionUntilNumberPickerControl.SelectItem(GameRoundSettings.modifierConditionSettings.timeUntil);
            modifierConditionFromNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue} %";
            modifierConditionUntilNumberPickerControl.GetLabelTextFunction = newValue => $"{newValue} %";
        }
        else if (GameRoundSettings.modifierConditionSettings.condition
            is EGameRoundModifierCondition.ScoreRange
            or EGameRoundModifierCondition.PlayerAdvance)
        {
            modifierConditionValues = NumberUtils.CreateIntList(0, 10000, 1000);
            modifierConditionFromNumberPickerControl.SelectItem(GameRoundSettings.modifierConditionSettings.scoreFrom);
            modifierConditionUntilNumberPickerControl.SelectItem(GameRoundSettings.modifierConditionSettings.scoreUntil);
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
            .Union(settings.PartyModeSettings.roundSettingsPresets)
            .ToList();
    }
}
