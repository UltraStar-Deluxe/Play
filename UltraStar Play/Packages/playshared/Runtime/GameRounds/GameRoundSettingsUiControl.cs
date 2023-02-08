using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameRoundSettingsUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = "modifierDialogOverlay")]
    private VisualElement modifierDialogOverlay;
    
    [Inject(UxmlName = "modifierChipsCombo")]
    private ChipsCombo modifierChipsCombo;

    [Inject]
    private Injector injector;
    
    private readonly GameRoundModifierDialogControl modifierDialogControl = new();
    private GameRoundModifierChipsComboControl modifierChipsComboControl;

    public IObservable<GameRoundSettings> GameRoundSettingsChangedEventStream => modifierChipsComboControl.GameRoundSettingsChangedEventStream;
    public IObservable<bool> DialogClosedEventStream => modifierDialogControl.DialogClosedEventStream;
    
    public GameRoundSettings GameRoundSettings
    {
        get { return modifierChipsComboControl.GameRoundSettings; }
        set { modifierChipsComboControl.GameRoundSettings = value; }
    } 

    public void OnInjectionFinished()
    {
        injector.WithRootVisualElement(modifierDialogOverlay)
            .Inject(modifierDialogControl);
        
        modifierChipsComboControl = new(modifierChipsCombo);
        modifierDialogControl.DialogClosedEventStream.Subscribe(_ => modifierChipsComboControl.UpdateChipsComboEntries());
        modifierChipsComboControl.ChipsCombo.ComboButton.RegisterCallbackButtonTriggered(() =>
        {
            modifierDialogControl.OpenDialog(GameRoundSettings);
        });
    }
}
