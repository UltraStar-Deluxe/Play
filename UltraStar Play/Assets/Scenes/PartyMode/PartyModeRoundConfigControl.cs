using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

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
    private GameRoundModifierDialogControl modifierDialogControl;

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

    [Inject(UxmlName = R.UxmlNames.modifierChipsCombo)]
    private ChipsCombo modifierChipsCombo;
    
    [Inject(UxmlName = R.UxmlNames.centerContent)]
    private VisualElement centerContent;

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

    private TextInputDialogControl savePresetDialogControl;
    public bool IsSavePresetDialogOpen => savePresetDialogControl != null;
    
    private bool IsFolded => visualElement.ClassListContains(FoldedClassName);

    private LabeledItemPickerControl<PartyModeRoundSettingsPreset> presetPickerControl;
    private LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl;
    private LabeledItemPickerControl<int> finishConditionPointsPickerControl;
    private GameRoundModifierChipsComboControl modifierChipsComboControl;

    private readonly Subject<GameRoundSettings> deletedEventStream = new();
    public IObservable<GameRoundSettings> DeletedEventStream => deletedEventStream;

    private readonly Subject<GameRoundSettings> foldEventStream = new();
    public IObservable<GameRoundSettings> FoldEventStream => foldEventStream;

    private readonly Subject<GameRoundSettings> unfoldEventStream = new();
    public IObservable<GameRoundSettings> UnfoldEventStream => unfoldEventStream;

    private readonly Subject<PartyModeRoundSettingsPreset> presetsChangedEventStream = new();
    public IObservable<PartyModeRoundSettingsPreset> PresetsChangedEventStream => presetsChangedEventStream;

    private float targetContentHeight = -1;
    
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
        modifierChipsComboControl.GameRoundSettings = GameRoundSettings;
        modifierChipsComboControl.GameRoundSettingsChangedEventStream.Subscribe(_ => UpdateControls());
        modifierChipsComboControl.ChipsCombo.ComboButton.RegisterCallbackButtonTriggered(() =>
        {
            modifierDialogControl.OpenDialog(GameRoundSettings);

            // Update controls if the dialog is closed
            IDisposable dialogClosedDisposable = null;
            dialogClosedDisposable = modifierDialogControl.DialogClosedEventStream.Subscribe(_ =>
            {
                UpdateControls();
                if (dialogClosedDisposable != null)
                {
                    dialogClosedDisposable.Dispose();
                    dialogClosedDisposable = null;
                }
            });
        });
            
        UpdateControls();
        UpdateTargetHeight();
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
        modifierChipsComboControl = new(modifierChipsCombo);
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
        CloseSavePresetDialog();
        
        VisualElement dialogVisualElement = valueInputDialogUi.CloneTreeAndGetFirstChild();
        dialogContainer.Add(dialogVisualElement);
        dialogVisualElement.AddToClassList("overlay");

        savePresetDialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<TextInputDialogControl>();
        savePresetDialogControl.Title = "Save preset";
        savePresetDialogControl.Message = "Enter preset name";

        savePresetDialogControl.SubmitValueEventStream
            .Subscribe(presetName => SavePreset(presetName));
        savePresetDialogControl.DialogClosedEventStream.Subscribe(_ =>
        {
            savePresetDialogControl = null;
        });
    }

    public void CloseSavePresetDialog()
    {
        if (savePresetDialogControl == null)
        {
            return;
        }
        savePresetDialogControl.CloseDialog();
        savePresetDialogControl = null;
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
        if (targetContentHeight >= 0)
        {
            centerContent.style.height = 0;
        }
        
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
        centerContent.style.height = targetContentHeight;

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
        modifierChipsComboControl.UpdateChipsComboEntries();

        UpdatePresetPicker();
        UpdatePresetPickerSaveDeleteButtons();
        
        UpdateTargetHeight();
    }

    private List<PartyModeRoundSettingsPreset> GetSelectablePresets()
    {
        return new List<PartyModeRoundSettingsPreset> { null }
            .Union(settings.PartyModeSettings.roundSettingsPresets)
            .ToList();
    }

    private void UpdateTargetHeight()
    {
        centerContent.style.height = new StyleLength(StyleKeyword.Auto);
        centerContent.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            targetContentHeight = evt.newRect.height;
            if (IsFolded)
            {
                centerContent.style.height = 0;
            }
            else
            {
                centerContent.style.height = targetContentHeight;
            }
        });
    }
}
