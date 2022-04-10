using System;
using UniInject;
using UnityEngine.UIElements;

public class MessageDialogControl : AbstractDialogControl
{
    [Inject(UxmlName = "dialogTitleImage")]
    public VisualElement DialogTitleImage { get; private set; }

    [Inject(UxmlName = "dialogTitle")]
    private Label dialogTitle;

    [Inject(UxmlName = "dialogMessage")]
    private Label dialogMessage;

    [Inject(UxmlName = "dialogButtonContainer")]
    private VisualElement dialogButtonContainer;

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

    public Button AddButton(string text, Action callback)
    {
        Button button = new();
        dialogButtonContainer.Add(button);

        button.text = text;
        button.focusable = true;
        button.RegisterCallbackButtonTriggered(callback);

        return button;
    }
}
