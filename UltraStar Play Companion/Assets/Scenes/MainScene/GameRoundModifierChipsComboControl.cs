using System;
using UniRx;
using UnityEngine.UIElements;

public class GameRoundModifierChipsComboControl
{
	public ChipsCombo ChipsCombo { get; private set; }

    public GameRoundSettingsDto gameRoundSettingsDto;
    public GameRoundSettingsDto GameRoundSettingsDto
    {
        get
        {
            return gameRoundSettingsDto;
        }
        set
        {
            gameRoundSettingsDto = value;
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
        if (GameRoundSettingsDto == null
            || GameRoundSettingsDto.ModifierDtos.IsNullOrEmpty())
        {
            return;
        }

        GameRoundSettingsDto.ModifierDtos.ForEach(modifierDto =>
            CreateChipsComboEntry(modifierDto.DisplayName, () => GameRoundSettingsDto.ModifierDtos.Remove(modifierDto)));
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
                gameRoundSettingsChangedEventStream.OnNext(gameRoundSettingsDto);
            });
        }
        else
        {
            button.HideByDisplay();
        }
    }
}
