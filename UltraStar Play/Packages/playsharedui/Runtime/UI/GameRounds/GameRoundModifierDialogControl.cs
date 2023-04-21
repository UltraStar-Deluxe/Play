using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniInject;
using UniRx;
using UnityEngine;
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

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierConditionRangeSlider)]
    private MinMaxSlider modifierConditionRangeSlider;

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierConditionRangeTextField)]
    private TextField modifierConditionRangeTextField;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.closeModifierDialogButton)]
    private Button closeModifierDialogButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.conditionTitleLabel)]
    private Label conditionTitleLabel;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.finishConditionPicker)]
    private ItemPicker finishConditionPicker;

    [Inject(UxmlName = R_PlayShared.UxmlNames.finishConditionPointsSlider)]
    private SliderInt finishConditionPointsSlider;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.finishConditionPointsTextField)]
    private IntegerField finishConditionPointsTextField;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.resetModifiersButton)]
    private Button resetModifiersButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.randomizeModifiersButton)]
    private Button randomizeModifiersButton;
    
    private GameRoundSettings gameRoundSettings;
    private GameRoundSettings GameRoundSettings => gameRoundSettings;

    private LabeledItemPickerControl<EGameRoundModifierCondition> modifierConditionPickerControl;
    private LabeledItemPickerControl<EGameRoundFinishCondition> finishConditionPickerControl;
    private BaseFieldWithTextFieldControl<Vector2> modifierConditionRangeBaseFieldWithTextFieldControl;
    
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

        randomizeModifiersButton.RegisterCallbackButtonTriggered(_ => Randomize());
        resetModifiersButton.RegisterCallbackButtonTriggered(_ => Reset());
        
        CreateControls();
        BindControls();
    }

    private void CreateControls()
    {
        // Finish condition
        finishConditionPickerControl = new(finishConditionPicker, EnumUtils.GetValuesAsList<EGameRoundFinishCondition>());
        finishConditionPickerControl.GetLabelTextFunction = item => StringUtils.ToTitleCase(item.ToString());
        finishConditionPointsSlider.lowValue = 100;
        finishConditionPointsSlider.highValue = 9900;
        new SliderIntStepControl(finishConditionPointsSlider, 100);
        new BaseFieldWithTextValueFieldControl<int>(finishConditionPointsSlider, finishConditionPointsTextField);

        // Modifiers
        UpdateGameRoundModifierToToggle();
        
        closeModifierDialogButton.RegisterCallbackButtonTriggered(_ => CloseDialog());
        
        modifierConditionPickerControl = new(modifierConditionPicker, EnumUtils.GetValuesAsList<EGameRoundModifierCondition>());
        modifierConditionPickerControl.GetLabelTextFunction = item => StringUtils.ToTitleCase(item.ToString());
        modifierConditionRangeBaseFieldWithTextFieldControl = new BaseFieldWithTextFieldControl<Vector2>(modifierConditionRangeSlider, modifierConditionRangeTextField,
            newValue =>
            {
                if (modifierConditionPickerControl.SelectedItem == EGameRoundModifierCondition.TimeRange)
                {
                    return $"{(int)newValue.x}% - {(int)newValue.y}%";
                }
                else
                {
                    return $"{(int)newValue.x} - {(int)newValue.y}";
                }
            },
            newText =>
            {
                string pattern = @"(?<fromValue>\d+)[\s\%]*-[\s\%]*(?<untilValue>\d+)[\s\%]*";
                Match match = Regex.Match(newText, pattern);
                if (match.Success)
                {
                    int fromValue = int.Parse(match.Groups["fromValue"].Value);
                    int untilValue = int.Parse(match.Groups["untilValue"].Value);
                    return new Vector2(fromValue, untilValue);
                }

                throw new ParseTextException($"Failed to parse {newText} into a range. Expected pattern: {pattern}");
            });

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

        FieldBindingUtils.Bind(finishConditionPointsSlider,
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

        // Modifier condition range
        modifierConditionRangeSlider.RegisterValueChangedCallback(evt =>
        {
            Vector2 newValue = evt.newValue;
            if (GameRoundSettings.modifierConditionSettings.condition
                is EGameRoundModifierCondition.ScoreRange
                or EGameRoundModifierCondition.PlayerAdvance)
            {
                GameRoundSettings.modifierConditionSettings.scoreFrom = (int)newValue.x;
                GameRoundSettings.modifierConditionSettings.scoreUntil = (int)newValue.y;
            }
            else if (GameRoundSettings.modifierConditionSettings.condition is EGameRoundModifierCondition.TimeRange)
            {
                GameRoundSettings.modifierConditionSettings.timeFrom = (int)newValue.x;
                GameRoundSettings.modifierConditionSettings.timeUntil = (int)newValue.y;
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
        finishConditionPointsSlider.value = GameRoundSettings.finishConditionSettings.points;
        finishConditionPointsSlider.SetVisibleByDisplay(GameRoundSettings.finishConditionSettings.condition != EGameRoundFinishCondition.ReachEndOfSong);
        finishConditionPointsTextField.SetVisibleByDisplay(finishConditionPointsSlider.IsVisibleByDisplay());
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
        modifierConditionRangeSlider.SetVisibleByDisplay(modifierConditionNumberPickersVisible
            || (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.PlayerAdvance
                && modifierConditionPickerControl.ItemPicker.IsVisibleByDisplay()));
        modifierConditionRangeTextField.SetVisibleByDisplay(modifierConditionNumberPickersVisible);
        modifierConditionRangeBaseFieldWithTextFieldControl.UpdateTextField(modifierConditionRangeSlider.value);

        if (GameRoundSettings.modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
        {
            modifierConditionRangeSlider.lowLimit = 0;
            modifierConditionRangeSlider.highLimit = 100;
            modifierConditionRangeSlider.value = new Vector2(
                GameRoundSettings.modifierConditionSettings.timeFrom,
                GameRoundSettings.modifierConditionSettings.timeUntil);
        }
        else if (GameRoundSettings.modifierConditionSettings.condition
            is EGameRoundModifierCondition.ScoreRange
            or EGameRoundModifierCondition.PlayerAdvance)
        {
            modifierConditionRangeSlider.lowLimit = 0;
            modifierConditionRangeSlider.highLimit = 10000;
            modifierConditionRangeSlider.value = new Vector2(
                GameRoundSettings.modifierConditionSettings.scoreFrom,
                GameRoundSettings.modifierConditionSettings.scoreUntil);
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
