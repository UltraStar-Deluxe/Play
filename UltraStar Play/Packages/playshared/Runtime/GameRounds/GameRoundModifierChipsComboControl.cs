using System;
using UniRx;
using UnityEngine.UIElements;

public class GameRoundModifierChipsComboControl
{
	public ChipsCombo ChipsCombo { get; private set; }

    public GameRoundSettings gameRoundSettings;
    public GameRoundSettings GameRoundSettings
    {
        get
        {
            return gameRoundSettings;
        }
        set
        {
            gameRoundSettings = value;
            UpdateChipsComboEntries();
        }
    }

    private readonly Subject<GameRoundSettings> gameRoundSettingsChangedEventStream = new();
    public IObservable<GameRoundSettings> GameRoundSettingsChangedEventStream => gameRoundSettingsChangedEventStream;

    public GameRoundModifierChipsComboControl(ChipsCombo chipsCombo)
    {
        chipsCombo.InitControl(this);
        this.ChipsCombo = chipsCombo;

        gameRoundSettingsChangedEventStream.Subscribe(_ => UpdateChipsComboEntries());
    }

    public void UpdateChipsComboEntries()
    {
        ChipsCombo.ChipsList.Clear();

        if (GameRoundSettings == null)
        {
            return;
        }

        // Add chips for finish condition
        if (GameRoundSettings.finishConditionSettings.condition is not EGameRoundFinishCondition.ReachEndOfSong)
        {
            CreateChipsComboEntry(GameRoundSettingsUtils.GetFinishConditionDescription(GameRoundSettings),
                () => GameRoundSettings.finishConditionSettings.condition = EGameRoundFinishCondition.ReachEndOfSong);

            if (!GameRoundSettings.UnconditionalModifiers.IsNullOrEmpty()
                || !GameRoundSettings.ConditionalModifiers.IsNullOrEmpty())
            {
                ChipsCombo.AddSeparator();
            }
        }

        // Add chips for unconditional modifiers
        if (!GameRoundSettings.UnconditionalModifiers.IsNullOrEmpty())
        {
            CreateGameRoundModifierChipsEntry(EGameRoundModifier.ShortSong, "Short song");
            CreateGameRoundModifierChipsEntry(EGameRoundModifier.PassTheMic, "Pass the mic");

            if (!GameRoundSettings.ConditionalModifiers.IsNullOrEmpty())
            {
                ChipsCombo.AddSeparator();
            }
        }

        // Add chips for conditional modifiers
        if (!GameRoundSettings.ConditionalModifiers.IsNullOrEmpty())
        {
            CreateGameRoundModifierChipsEntry(EGameRoundModifier.HideLyrics, "Hide lyrics");
            CreateGameRoundModifierChipsEntry(EGameRoundModifier.HideNotes, "Hide notes");
            CreateGameRoundModifierChipsEntry(EGameRoundModifier.ReduceAudio, "Reduce audio");

            if (GameRoundSettings.modifierConditionSettings.condition is not EGameRoundModifierCondition.Always)
            {
                CreateChipsComboEntry(GameRoundSettingsUtils.GetModifierConditionDescription(GameRoundSettings),
                    () => GameRoundSettings.modifierConditionSettings.condition = EGameRoundModifierCondition.Always);
            }
        }
    }
    
    private void CreateChipsComboEntry(string labelText, Action onRemove = null)
    {
        VisualElement chipsComboEntryVisualElement = VisualElementUtils.LoadVisualElementFromResources("UIDocuments/ChipsComboEntry");
        ChipsCombo.ChipsList.Add(chipsComboEntryVisualElement);

        Label label = chipsComboEntryVisualElement.Q<Label>("chipsComboEntryLabel");
        label.text = labelText;

        Button button = chipsComboEntryVisualElement.Q<Button>("chipsComboEntryButton");
        if (onRemove != null)
        {
            button.RegisterCallbackButtonTriggered(() =>
            {
                onRemove();
                gameRoundSettingsChangedEventStream.OnNext(gameRoundSettings);
            });
        }
        else
        {
            button.HideByDisplay();
        }
    }

    private void CreateGameRoundModifierChipsEntry(EGameRoundModifier gameRoundModifier, string labelText)
    {
        if (GameRoundSettings.modifiers.Contains(gameRoundModifier))
        {
            CreateChipsComboEntry(labelText, () => GameRoundSettings.modifiers.Remove(gameRoundModifier));
        }
    }
}
