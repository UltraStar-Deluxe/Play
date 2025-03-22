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

    [Inject(UxmlName = R_PlayShared.UxmlNames.resetModifiersButton)]
    private Button resetModifiersButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.randomizeModifiersButton)]
    private Button randomizeModifiersButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.modifierContainer)]
    private VisualElement modifierContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.closeModifierDialogButton)]
    private Button closeModifierDialogButton;

    private GameRoundSettings gameRoundSettings;
    private GameRoundSettings GameRoundSettings => gameRoundSettings;

    private readonly Subject<VoidEvent> dialogClosedEventStream = new();
    public IObservable<VoidEvent> DialogClosedEventStream => dialogClosedEventStream;

    public bool IsVisible => visualElement.IsVisibleByDisplay();

    private bool isInitialized;

    private readonly List<GameRoundModifierDialogEntryControl> entryControls = new();

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
        entryControls.Clear();
        List<IGameRoundModifier> gameRoundModifiers = GameRoundModifierRegistry.GetAll()
            .OrderBy(modifier => modifier.DisplayOrder)
            .ToList();
        foreach (IGameRoundModifier gameRoundModifier in gameRoundModifiers)
        {
            CreateGameRoundModifierControl(gameRoundModifier);
        }
    }

    private void CreateGameRoundModifierControl(IGameRoundModifier modifier)
    {
        GameRoundModifierDialogEntryControl entryControl = new(modifier, gameRoundSettings);
        entryControls.Add(entryControl);
        modifierContainer.Add(entryControl.VisualElement);
    }

    private void Randomize()
    {
        foreach (GameRoundModifierDialogEntryControl entryControl in entryControls)
        {
            entryControl.Randomize();
        }
    }

    private void Reset()
    {
        foreach (GameRoundModifierDialogEntryControl entryControl in entryControls)
        {
            entryControl.Reset();
        }
    }

    public void CloseDialog()
    {
        visualElement.HideByDisplay();
        gameRoundSettings = null;
        dialogClosedEventStream.OnNext(VoidEvent.instance);
    }

    public void OpenDialog(GameRoundSettings newGameRoundSettings)
    {
        visualElement.ShowByDisplay();
        gameRoundSettings = newGameRoundSettings;
        Init();
    }
}
