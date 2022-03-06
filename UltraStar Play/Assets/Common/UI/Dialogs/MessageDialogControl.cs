using System;
using System.Linq;
using UniRx;
using UnityEngine.UIElements;

public class MessageDialogControl : IDialogControl
{
    private readonly VisualElement dialogRootVisualElement;
    private readonly VisualElement parentVisualElement;

    private readonly VisualElement buttonContainer;
    public VisualElement DialogTitleImage { get; private set; }

    private readonly Subject<bool> dialogClosedEventStream = new Subject<bool>();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    public MessageDialogControl(
        VisualTreeAsset dialogUi,
        VisualElement parentVisualElement,
        string title,
        string message)
    {
        dialogRootVisualElement = dialogUi.CloneTree();
        dialogRootVisualElement.AddToClassList("overlay");

        buttonContainer = dialogRootVisualElement.Q<VisualElement>(R.UxmlNames.dialogButtonContainer);
        DialogTitleImage = dialogRootVisualElement.Q<VisualElement>(R.UxmlNames.dialogTitleImage);
        Label dialogTitle = dialogRootVisualElement.Q<Label>(R.UxmlNames.dialogTitle);
        Label dialogMessage = dialogRootVisualElement.Q<Label>(R.UxmlNames.dialogMessage);

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
        dialogClosedEventStream.OnNext(true);
    }
}
