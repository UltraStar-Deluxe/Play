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

    public void AddButton(Button button)
    {
        dialogButtonContainer.Add(button);

        button.focusable = true;
        button.Focus();
        // Sometimes Unity cannot focus the button until it has been rendered once.
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
            () => button.Focus()));
    }

    public Button AddButton(string text, EventCallback<EventBase> callback)
    {
        Button button = new();

        button.text = text;
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
