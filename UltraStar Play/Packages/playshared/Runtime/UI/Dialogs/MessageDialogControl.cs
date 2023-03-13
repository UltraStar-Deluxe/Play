using System;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class MessageDialogControl : AbstractDialogControl, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogTitleImage)]
    public VisualElement DialogTitleImage { get; private set; }

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogTitle)]
    private Label dialogTitle;

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogMessageContainer)]
    private VisualElement dialogMessageContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogMessage)]
    private Label dialogMessage;

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogButtonContainer)]
    private VisualElement dialogButtonContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.defaultCloseDialogButton)]
    private Button defaultCloseDialogButton;
    
    [Inject]
    private Injector injector;

    private VisualElement lastFocusedVisualElement;

    public string Title
    {
        get
        {
            return dialogTitle.text;
        }

        set
        {
            dialogTitle.text = value;
        }
    }

    public string Message
    {
        get
        {
            return dialogMessage.text;
        }

        set
        {
            dialogMessage.text = value;
        }
    }

    public void OnInjectionFinished()
    {
        lastFocusedVisualElement = DialogRootVisualElement.focusController.focusedElement as VisualElement;
        
        dialogTitle.text = "";
        dialogMessage.text = "";

        // Close dialog using "back" InputAction with high priority
        disposables.Add(InputManager.GetInputAction("usplay/back").PerformedAsObservable(100).Subscribe(context =>
        {
            CloseDialog();
            InputManager.GetInputAction("usplay/back").CancelNotifyForThisFrame();
        }));
        
        // Close by clicking on background
        VisualElementUtils.RegisterDirectClickCallback(DialogRootVisualElement, () => CloseDialog());
        
        // Close by clicking on default close button
        defaultCloseDialogButton.RegisterCallbackButtonTriggered(() => CloseDialog());
        defaultCloseDialogButton.Focus();
    }

    public Button AddButton(string text, Action callback)
    {
        Button button = new();
        dialogButtonContainer.Add(button);

        button.text = text;
        button.focusable = true;
        button.RegisterCallbackButtonTriggered(callback);

        button.Focus();
        
        return button;
    }

    public void AddVisualElement(VisualElement visualElement)
    {
        dialogMessageContainer.Add(visualElement);
    }

    public override void CloseDialog()
    {
        base.CloseDialog();
        if (lastFocusedVisualElement != null)
        {
            lastFocusedVisualElement.Focus();
        }
    }
}
