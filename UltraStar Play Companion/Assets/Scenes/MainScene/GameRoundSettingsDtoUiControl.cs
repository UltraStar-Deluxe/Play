using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameRoundSettingsDtoUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = "modifierDialogOverlay")]
    private VisualElement modifierDialogOverlay;

    [Inject(UxmlName = "modifierChipsCombo")]
    private ChipsCombo modifierChipsCombo;

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierContainer)]
    private VisualElement modifierContainer;

    [Inject]
    private Injector injector;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    private readonly GameRoundModifierDtoDialogControl modifierDtoDialogControl = new();
    private GameRoundModifierChipsComboControl modifierChipsComboControl;

    public IObservable<GameRoundSettingsDto> GameRoundSettingsChangedEventStream => modifierChipsComboControl.GameRoundSettingsChangedEventStream;
    public IObservable<bool> DialogClosedEventStream => modifierDtoDialogControl.DialogClosedEventStream;

    private List<GameRoundModifierDto> availableGameRoundModifierDtos = new();

    public GameRoundSettingsDto GameRoundSettingsDto
    {
        get { return modifierChipsComboControl.GameRoundSettingsDto; }
        set { modifierChipsComboControl.GameRoundSettingsDto = value; }
    }

    public void OnInjectionFinished()
    {
        injector.WithRootVisualElement(modifierDialogOverlay)
            .Inject(modifierDtoDialogControl);

        clientSideCompanionClientManager.ConnectEventStream
            .Subscribe(evt =>
            {
                if (evt.IsSuccess)
                {
                    availableGameRoundModifierDtos = evt.AvailableGameRoundModifierDtos;
                }
            });

        modifierChipsComboControl = new(modifierChipsCombo);
        modifierDtoDialogControl.DialogClosedEventStream
            .Subscribe(_ => modifierChipsComboControl.UpdateChipsComboEntries());
        modifierChipsComboControl.ChipsCombo.ComboButton
            .RegisterCallbackButtonTriggered(_ => modifierDtoDialogControl.OpenDialog(GameRoundSettingsDto, availableGameRoundModifierDtos));
    }

    public void CloseModifierDialog()
    {
        modifierDtoDialogControl.CloseDialog();
    }
}
