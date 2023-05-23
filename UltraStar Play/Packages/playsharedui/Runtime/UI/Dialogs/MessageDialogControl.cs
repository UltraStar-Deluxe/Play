using System;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class MessageDialogControl : AbstractModalDialogControl, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogTitleImage)]
    public VisualElement DialogTitleImage { get; protected set; }

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogTitle)]
    protected Label dialogTitle;

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogMessageContainer)]
    protected VisualElement dialogMessageContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogMessage)]
    protected Label dialogMessage;
    public Label MessageElement => dialogMessage;

    [Inject(UxmlName = R_PlayShared.UxmlNames.dialogButtonContainer)]
    protected VisualElement dialogButtonContainer;

    [Inject]
    protected Injector injector;

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

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        
        dialogTitle.text = "";
        dialogMessage.text = "";
    }

    public Button AddButton(string text, EventCallback<EventBase> callback)
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
