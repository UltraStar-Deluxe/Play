using System;
using UnityEngine.UIElements;

public class SimpleDialogControl
{
    private readonly VisualElement dialogRootVisualElement;
    private readonly VisualElement parentVisualElement;

    private readonly VisualElement buttonContainer;
    public VisualElement DialogTitleImage { get; private set; }

    public SimpleDialogControl(
        VisualTreeAsset dialogUi,
        VisualElement parentVisualElement,
        string title,
        string message)
    {
        dialogRootVisualElement = dialogUi.CloneTree();
        buttonContainer = dialogRootVisualElement.Q<VisualElement>(R.UxmlNames.dialogButtonContainer);
        DialogTitleImage = dialogRootVisualElement.Q<VisualElement>(R.UxmlNames.dialogTitleImage);
        Label dialogTitle = dialogRootVisualElement.Q<Label>(R.UxmlNames.dialogTitle);
        Label dialogMessage = dialogRootVisualElement.Q<Label>(R.UxmlNames.dialogMessage);

        dialogRootVisualElement.AddToClassList("overlay");
        dialogTitle.text = title;
        dialogMessage.text = message;

        this.parentVisualElement = parentVisualElement;
        parentVisualElement.Add(dialogRootVisualElement);
    }

    public Button AddButton(string text, Action callback)
    {
        Button button = new Button();
        buttonContainer.Add(button);

        button.text = text;
        button.focusable = true;
        button.RegisterCallbackButtonTriggered(callback);

        return button;
    }

    public TextField AddTextField()
    {
        TextField textField = new TextField();
        buttonContainer.Add(textField);
        return textField;
    }

    public void CloseDialog()
    {
        parentVisualElement.Remove(dialogRootVisualElement);
    }
}
