using System;
using UnityEngine.UIElements;

public class SimpleUxmlDialog
{
    private readonly VisualElement dialogRootVisualElement;
    private readonly VisualElement parentVisualElement;

    private readonly VisualElement buttonContainer;

    public SimpleUxmlDialog(
        VisualTreeAsset dialogUxml,
        VisualElement parentVisualElement,
        string title,
        string message)
    {
        dialogRootVisualElement = dialogUxml.CloneTree();
        buttonContainer = dialogRootVisualElement.Q<VisualElement>("dialogButtonContainer");
        Label dialogTitle = dialogRootVisualElement.Q<Label>("dialogTitle");
        Label dialogMessage = dialogRootVisualElement.Q<Label>("dialogMessage");

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

    public void CloseDialog()
    {
        parentVisualElement.Remove(dialogRootVisualElement);
    }
}
