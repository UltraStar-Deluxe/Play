using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class GameRoundModifierDtoDialogControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R_PlayShared.UxmlNames.resetModifiersButton)]
    private Button resetModifiersButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.randomizeModifiersButton)]
    private Button randomizeModifiersButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierContainer)]
    private VisualElement modifierContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.closeModifierDialogButton)]
    private Button closeModifierDialogButton;

    private GameRoundSettingsDto gameRoundSettingsDto;

    private List<GameRoundModifierDto> availableGameRoundModifierDtos;

    private readonly Subject<bool> dialogClosedEventStream = new();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

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
        closeModifierDialogButton.RegisterCallbackButtonTriggered(_ => CloseDialog());

        CreateControls();
    }

    private void CreateControls()
    {
        modifierContainer.Clear();
        foreach (GameRoundModifierDto modifierDto in availableGameRoundModifierDtos)
        {
            CreateGameRoundModifierDtoControl(modifierDto);
        }
    }

    private void CreateGameRoundModifierDtoControl(GameRoundModifierDto modifierDto)
    {
        SlideToggle toggle = new();
        toggle.name = $"{modifierDto.Id}Toggle";
        toggle.AddToClassList("gameRoundModifierToggle");
        toggle.label = modifierDto.DisplayName;
        toggle.RegisterValueChangedCallback(evt=>
        {
            if (evt.newValue)
            {
                gameRoundSettingsDto.AddModifier(modifierDto);
            }
            else
            {
                gameRoundSettingsDto.RemoveModifierById(modifierDto.Id);
            }
        });
        toggle.userData = modifierDto;

        modifierContainer.Add(toggle);
    }

    private void Randomize()
    {
        modifierContainer.Query<SlideToggle>()
            .ForEach(toggle => toggle.value = RandomUtils.RandomTrue());
    }

    private void Reset()
    {
        modifierContainer.Query<SlideToggle>()
            .ForEach(toggle => toggle.value = false);
    }

    public void CloseDialog()
    {
        visualElement.HideByDisplay();
        gameRoundSettingsDto = null;
        dialogClosedEventStream.OnNext(true);
    }

    public void OpenDialog(GameRoundSettingsDto newGameRoundSettings, List<GameRoundModifierDto> newAvailableGameRoundModifierDtos)
    {
        visualElement.ShowByDisplay();
        gameRoundSettingsDto = newGameRoundSettings;
        availableGameRoundModifierDtos = newAvailableGameRoundModifierDtos;
        Init();
        UpdateControls();
    }

    private void UpdateControls()
    {
        modifierContainer.Query<SlideToggle>()
            .ForEach(toggle =>
            {
                GameRoundModifierDto modifier = toggle.userData as GameRoundModifierDto;
                return toggle.value = gameRoundSettingsDto.ContainsModifierWithId(modifier?.Id);
            });
    }
}
