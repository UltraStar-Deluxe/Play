using System;
using UniRx;
using UnityEngine.UIElements;

public class GameRoundModifierChipsComboControl
{
	public ChipsCombo ChipsCombo { get; private set; }

    public GameRoundSettingsDto gameRoundSettings;
    public GameRoundSettingsDto GameRoundSettings
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

    private readonly Subject<GameRoundSettingsDto> gameRoundSettingsChangedEventStream = new();
    public IObservable<GameRoundSettingsDto> GameRoundSettingsChangedEventStream => gameRoundSettingsChangedEventStream;

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

        // Add chips for modifiers
        CreateGameRoundModifierChipsEntry(EGameRoundModifier.HideLyrics, "Hide lyrics");
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
            button.RegisterCallbackButtonTriggered(_ =>
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
