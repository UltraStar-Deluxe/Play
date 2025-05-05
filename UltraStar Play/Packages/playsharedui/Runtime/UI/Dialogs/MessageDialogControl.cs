using UniInject;
using UnityEngine;
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

    public Translation Title
    {
        get
        {
            return Translation.Of(dialogTitle.text);
        }

        set
        {
            dialogTitle.SetTranslatedText(value);
        }
    }

    public Translation Message
    {
        get
        {
            return Translation.Of(dialogMessage.text);
        }

        set
        {
            dialogMessage.SetTranslatedText(value);
        }
    }

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        dialogTitle.SetTranslatedText(Translation.Empty);
        dialogMessage.SetTranslatedText(Translation.Empty);
    }

    public async void AddButton(Button button)
    {
        dialogButtonContainer.Add(button);

        button.focusable = true;
        button.Focus();

        // Sometimes Unity cannot focus the button until it has been rendered once.
        await Awaitable.NextFrameAsync();
        button.Focus();
    }

    public Button AddButton(Translation text, EventCallback<EventBase> callback)
    {
        return AddButton(text, "", callback);
    }

    public Button AddButton(Translation text, string name, EventCallback<EventBase> callback)
    {
        Button button = new();
        button.name = name;

        button.SetTranslatedText(text);
        button.RegisterCallbackButtonTriggered(callback);

        AddButton(button);

        return button;
    }

    public void AddVisualElement(VisualElement visualElement)
    {
        dialogMessageContainer.Add(visualElement);
    }

    public void AddInformationMessage(string informationMessage)
    {
        VisualElement infoContainer = new();
        infoContainer.name = "row";
        infoContainer.AddToClassList("ml-auto");
        infoContainer.AddToClassList("mr-auto");
        infoContainer.AddToClassList("my-3");

        FontIcon infoIcon = new MaterialIcon();
        infoIcon.Icon = "info_outline";
        infoIcon.style.fontSize = 14;
        infoIcon.AddToClassList("mr-1");
        infoContainer.Add(infoIcon);

        Label infoLabel = new Label(informationMessage);
        infoLabel.AddToClassList("smallFont");
        infoContainer.Add(infoLabel);

        AddVisualElement(infoContainer);
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
