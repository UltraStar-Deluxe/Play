using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public abstract class AbstractModalDialogControl : AbstractDialogControl, INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.defaultCloseDialogButton, Optional = true)]
    protected Button defaultCloseDialogButton;

    protected VisualElement lastFocusedVisualElement;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        lastFocusedVisualElement = DialogRootVisualElement.focusController.focusedElement as VisualElement;

        // Close dialog using "back" InputAction with high priority.
        // Dialogs that opened later have higher priority, so they will be closed first.
        disposables.Add(InputManager.GetInputAction("usplay/back").PerformedAsObservable(100 + instantiatedDialogCount)
            .Subscribe(context =>
            {
                CloseDialog();
                InputManager.GetInputAction("usplay/back").CancelNotifyForThisFrame();
            }));

        // Close by clicking on background
        VisualElementUtils.RegisterDirectClickCallback(DialogRootVisualElement, () => CloseDialog());

        // Close by clicking on default close button
        defaultCloseDialogButton?.RegisterCallbackButtonTriggered(_ => CloseDialog());
        defaultCloseDialogButton?.Focus();
    }
}
