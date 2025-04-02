using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongSelectModifiersControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.toggleModifiersOverlayButton)]
    private Button toggleModifiersOverlayButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.resetModifiersButton)]
    private Button resetModifiersButton;

    [Inject(UxmlName = R.UxmlNames.modifiersActiveIcon)]
    private VisualElement modifiersActiveIcon;

    [Inject(UxmlName = R.UxmlNames.hiddenHideModifiersOverlayArea)]
    private VisualElement hiddenHideModifiersOverlayArea;

    [Inject(UxmlName = R.UxmlNames.modifiersInactiveIcon)]
    private VisualElement modifiersInactiveIcon;

    [Inject(UxmlName = R.UxmlNames.closeModifiersOverlayButton)]
    private Button closeModifiersOverlayButton;

    [Inject(UxmlName = R.UxmlNames.modifierDialogOverlay)]
    private VisualElement modifierDialogOverlay;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private Injector injector;

    private readonly GameRoundModifierDialogControl modifierDialogControl = new();

    public VisualElementSlideInControl ModifiersOverlaySlideInControl { get; private set; }

    private bool AnyModifierOrCoopModeActive => nonPersistentSettings.GameRoundSettings.AnyModifierActive ||
                                                SettingsUtils.IsCoopModeEnabled(settings);

    public void OnInjectionFinished()
    {
        ModifiersOverlaySlideInControl = new(modifierDialogOverlay, ESide2D.Right, false);
        SongSelectSlideInControlUtils.InitSlideInControl(ModifiersOverlaySlideInControl, toggleModifiersOverlayButton, closeModifiersOverlayButton, modifierDialogOverlay, hiddenHideModifiersOverlayArea);

        resetModifiersButton.RegisterCallbackButtonTriggered(_ => ResetCoopMode());

        InitModifierDialog();
    }

    private void ResetCoopMode()
    {
        if (SettingsUtils.IsCoopModeEnabled(settings))
        {
            SettingsUtils.SetCoopModeEnabled(settings, false);
        }
    }

    private void InitModifierDialog()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitModifierDialog");

        // Modifier active icon
        modifiersActiveIcon.HideByDisplay();
        nonPersistentSettings.ObserveEveryValueChanged(_ => AnyModifierOrCoopModeActive)
            .Subscribe(_ => UpdateModifiersActiveIcon());

        // Delay initialization of modifier dialog control
        bool initializedModifierDialogControl = false;
        ModifiersOverlaySlideInControl.Visible.Subscribe(newValue =>
        {
            if (newValue
                && !initializedModifierDialogControl)
            {
                initializedModifierDialogControl = true;
                InitModifierDialogControl();
            }
        });
    }

    private void InitModifierDialogControl()
    {
        injector.WithRootVisualElement(modifierDialogOverlay)
            .Inject(modifierDialogControl);
        modifierDialogControl.OpenDialog(nonPersistentSettings.GameRoundSettings);
        modifierDialogOverlay.Query(R_PlayShared.UxmlNames.closeModifierDialogButton).ForEach(it => it.HideByDisplay());
    }

    private void UpdateModifiersActiveIcon()
    {
        modifiersActiveIcon.SetVisibleByDisplay(AnyModifierOrCoopModeActive);
        modifiersInactiveIcon.SetVisibleByDisplay(!AnyModifierOrCoopModeActive);
    }
}
