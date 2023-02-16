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
        
        void CreateChipsComboEntry(
            string labelText,
            Action onRemove = null)
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

        void CreateBoolChipsEntry(EGameRoundModifier gameRoundModifier, string labelText)
        {
            if (GameRoundSettings.modifiers.Contains(gameRoundModifier))
            {
                CreateChipsComboEntry(labelText, () => GameRoundSettings.modifiers.Remove(gameRoundModifier));
            }
        }

        // Add chips
        if (!GameRoundSettings.UnconditionalModifiers.IsNullOrEmpty())
        {
            CreateBoolChipsEntry(EGameRoundModifier.ShortSong, "Short song");
            CreateBoolChipsEntry(EGameRoundModifier.PassTheMic, "Pass the mic");

            if (!GameRoundSettings.ConditionalModifiers.IsNullOrEmpty())
            {
                ChipsCombo.AddSeparator();
            }
        }

        if (!GameRoundSettings.ConditionalModifiers.IsNullOrEmpty())
        {
            CreateBoolChipsEntry(EGameRoundModifier.HideLyrics, "Hide lyrics");
            CreateBoolChipsEntry(EGameRoundModifier.HideNotes, "Hide notes");
            CreateBoolChipsEntry(EGameRoundModifier.ReduceAudio, "Reduce audio");

            if (GameRoundSettings.modifierConditionSettings.condition is not EGameRoundModifierCondition.Always)
            {
                CreateChipsComboEntry(GameRoundSettingsUtils.GetModifierConditionDescription(GameRoundSettings),
                    () => GameRoundSettings.modifierConditionSettings.condition = EGameRoundModifierCondition.Always);
            }
        }
    }
}
