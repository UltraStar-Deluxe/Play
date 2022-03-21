using System;
using UniInject;
using UnityEngine.UIElements;

public class MessageDialogControl : AbstractDialogControl
{
    [Inject(UxmlName = R.UxmlNames.dialogTitleImage)]
    public VisualElement DialogTitleImage { get; private set; }

    [Inject(UxmlName = R.UxmlNames.dialogTitle)]
    private Label dialogTitle;

    [Inject(UxmlName = R.UxmlNames.dialogMessage)]
    private Label dialogMessage;

    [Inject(UxmlName = R.UxmlNames.dialogButtonContainer)]
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
        Button button = new Button();
        dialogButtonContainer.Add(button);

        button.text = text;
        button.focusable = true;
        button.RegisterCallbackButtonTriggered(callback);

        return button;
    }
}
