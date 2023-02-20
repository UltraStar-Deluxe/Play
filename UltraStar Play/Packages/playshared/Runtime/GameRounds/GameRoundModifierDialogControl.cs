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
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.finishConditionPicker)]
    private ItemPicker finishConditionPicker;

    [Inject(UxmlName = R_PlayShared.UxmlNames.finishConditionPointsPicker)]
    private ItemPicker finishConditionPointsPicker;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.resetModifiersButton)]
    private Button resetModifiersButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.randomizeModifiersButton)]
    private Button randomizeModifiersButton;
    
    private GameRoundSettings gameRoundSettings;
    private GameRoundSettings GameRoundSettings => gameRoundSettings;

    private LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl;
    private LabeledItemPickerControl<int> modifierConditionFromNumberPickerControl;
    private LabeledItemPickerControl<int> modifierConditionUntilNumberPickerControl;
    private LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl;
    private LabeledItemPickerControl<int> finishConditionPointsPickerControl;
    
    private readonly Subject<bool> dialogClosedEventStream = new();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    private readonly Dictionary<EGameRoundModifier, Toggle> gameRoundModifierToToggle = new();

    public bool IsVisible => visualElement.IsVisibleByDisplay();
    
    private bool isInitialized;
    
    public void OnInjectionFinished()
    {
        visualElement.HideByDisplay();
    }

    private void Init()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        randomizeModifiersButton.RegisterCallbackButtonTriggered(() => Randomize());
        resetModifiersButton.RegisterCallbackButtonTriggered(() => Reset());
        
        CreateControls();
        BindControls();
    }

    private void CreateControls()
    {
        // Finish condition
        finishConditionPickerControl = new(finishConditionPicker, EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        finishConditionPointsPickerControl = new(finishConditionPointsPicker, NumberUtils.CreateIntList(1000, 9000, 1000));
        
        // Modifiers
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
    }

    private void BindControls()
    {
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
        
        // Modifier condition
        modifierConditionPickerControl.Selection.Subscribe(newValue =>
        {
            GameRoundSettings.modifierConditionSettings.condition = newValue;
            UpdateControls();
        });

        // Modifier condition from
        modifierConditionFromNumberPickerControl.Selection.Subscribe(newValue =>
        {
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

    private void Randomize()
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
    
    private void Reset()
    {
        GameRoundSettings.finishConditionSettings = new();

        GameRoundSettings.modifiers = new();
        GameRoundSettings.modifierConditionSettings.condition = new();

        UpdateControls();
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
        UpdateFinishConditionControls();
        UpdateModifierControls();
    }

    private void UpdateFinishConditionControls()
    {
        // Finish condition
        finishConditionPickerControl.SelectItem(GameRoundSettings.finishConditionSettings.condition);
        finishConditionPointsPickerControl.SelectItem(GameRoundSettings.finishConditionSettings.points);
        finishConditionPointsPickerControl.ItemPicker.SetVisibleByDisplay(GameRoundSettings.finishConditionSettings.condition != EGameRoundFinishCondition.ReachEndOfSong);
    }

    private void UpdateModifierControls()
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
        Init();
        UpdateControls();
    }
}
