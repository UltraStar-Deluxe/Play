using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameRoundModifierDialogControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R_PlayShared.UxmlNames.shortSongToggle)]
    private Toggle shortSongToggle;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.passTheMicToggle)]
    private Toggle passTheMicToggle;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.hideLyricsToggle)]
    private Toggle hideLyricsToggle;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.hideNotesToggle)]
    private Toggle hideNotesToggle;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.reduceAudioToggle)]
    private Toggle reduceAudioToggle;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierConditionPicker)]
    private ItemPicker modifierConditionPicker;

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierConditionFromNumberPicker)]
    private ItemPicker modifierConditionFromNumberPicker;

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierConditionUntilNumberPicker)]
    private ItemPicker modifierConditionUntilNumberPicker;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.closeModifierDialogButton)]
    private Button closeModifierDialogButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.conditionTitleLabel)]
    private Label conditionTitleLabel;
    
    private GameRoundSettings gameRoundSettings;
    private GameRoundSettings GameRoundSettings => gameRoundSettings;

    private LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl;
    private LabeledItemPickerControl<int> modifierConditionFromNumberPickerControl;
    private LabeledItemPickerControl<int> modifierConditionUntilNumberPickerControl;

    private readonly Subject<bool> dialogClosedEventStream = new();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    private readonly Dictionary<EGameRoundModifier, Toggle> gameRoundModifierToToggle = new();

    public bool IsVisible => visualElement.IsVisibleByDisplay();
    
    public void OnInjectionFinished()
    {
        visualElement.HideByDisplay();
        
        UpdateGameRoundModifierToToggle();
        
        closeModifierDialogButton.RegisterCallbackButtonTriggered(() => CloseDialog());
        
        modifierConditionPickerControl = new(modifierConditionPicker, EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());
        modifierConditionFromNumberPickerControl = new(modifierConditionFromNumberPicker, new List<int> { 0 });
        modifierConditionUntilNumberPickerControl = new(modifierConditionUntilNumberPicker, new List<int> { 0 });
        
        // Modifier enum toggles
        gameRoundModifierToToggle.ForEach(entry =>
        {
            EGameRoundModifier gameRoundModifier = entry.Key;
            Toggle toggle = entry.Value;
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (gameRoundSettings == null)
                {
                    return;
                }

                if (evt.newValue)
                {
                    gameRoundSettings.modifiers.Add(gameRoundModifier);
                }
                else
                {
                    gameRoundSettings.modifiers.Remove(gameRoundModifier);
                }
                UpdateControls();
            });
        });
        
        // Modifier condition
        modifierConditionPickerControl.Selection.Subscribe(newValue =>
        {
            if (GameRoundSettings == null)
            {
                return;
            }
            GameRoundSettings.modifierConditionSettings.condition = newValue;
            UpdateControls();
        });

        // Modifier condition from
        modifierConditionFromNumberPickerControl.Selection.Subscribe(newValue =>
        {
            if (GameRoundSettings == null)
            {
                return;
            }
            
            if (GameRoundSettings.modifierConditionSettings.condition
                is EGameRoundModifierCondition.ScoreRange
                or EGameRoundModifierCondition.PlayerAdvance)
            {
                GameRoundSettings.modifierConditionSettings.scoreFrom = newValue;
                UpdateControls();
            }
            else if (GameRoundSettings.modifierConditionSettings.condition is EGameRoundModifierCondition.TimeRange)
            {
                GameRoundSettings.modifierConditionSettings.timeFrom = newValue;
                UpdateControls();
            }
        });

        // Modifier condition until
        modifierConditionUntilNumberPickerControl.Selection.Subscribe(newValue =>
        {
            if (GameRoundSettings == null)
            {
                return;
            }

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
    }

    private void UpdateGameRoundModifierToToggle()
    {
        gameRoundModifierToToggle.Clear();
        gameRoundModifierToToggle[EGameRoundModifier.ShortSong] = shortSongToggle;
        gameRoundModifierToToggle[EGameRoundModifier.PassTheMic] = passTheMicToggle;
        gameRoundModifierToToggle[EGameRoundModifier.HideLyrics] = hideLyricsToggle;
        gameRoundModifierToToggle[EGameRoundModifier.HideNotes] = hideNotesToggle;
        gameRoundModifierToToggle[EGameRoundModifier.ReduceAudio] = reduceAudioToggle;
    }

    private void UpdateControls()
    {
        // Modifier enum toggles
        gameRoundModifierToToggle.ForEach(entry =>
        {
            EGameRoundModifier gameRoundModifier = entry.Key;
            Toggle toggle = entry.Value;
            toggle.value = GameRoundSettings.modifiers.Contains(gameRoundModifier);
        });
        
        // Modifier condition
        bool conditionVisible = !GameRoundSettings.ConditionalModifiers.IsNullOrEmpty();
        conditionTitleLabel.SetVisibleByDisplay(conditionVisible);
        modifierConditionPickerControl.ItemPicker.SetVisibleByDisplay(conditionVisible);
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
    }

    public void CloseDialog()
    {
        visualElement.HideByDisplay();
        gameRoundSettings = null;
        dialogClosedEventStream.OnNext(true);
    }

    public void OpenDialog(GameRoundSettings newGameRoundSettings)
    {
        visualElement.ShowByDisplay();
        gameRoundSettings = newGameRoundSettings;
        UpdateControls();
    }
}
