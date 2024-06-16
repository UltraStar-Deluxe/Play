using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class ContextMenuItemControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.contextMenuLabel)]
    private Label label;

    [Inject(UxmlName = R_PlayShared.UxmlNames.contextMenuButton)]
    private Button button;

    [Inject(UxmlName = R_PlayShared.UxmlNames.contextMenuButtonIcon)]
    private MaterialIcon iconElement;

    private readonly Translation text;
    private readonly string icon;
    private readonly string buttonName;
    private readonly Action action;

    private readonly Subject<VoidEvent> itemTriggeredEventStream = new();
    public IObservable<VoidEvent> ItemTriggeredEventStream => itemTriggeredEventStream;

    public ContextMenuItemControl(Translation text, string icon, string buttonName, Action action)
    {
        this.text = text;
        this.icon = icon;
        this.buttonName = buttonName;
        this.action = action;
    }

    public void OnInjectionFinished()
    {
        if (icon.IsNullOrEmpty())
        {
            iconElement.HideByDisplay();
        }
        else
        {
            iconElement.ShowByDisplay();
            iconElement.Icon = icon;
        }

        label.SetTranslatedText(text);
        button.name = buttonName;
        button.RegisterCallbackButtonTriggered(_ =>
        {
            if (button.focusController.focusedElement == button)
            {
                // Workaround for Unity issue "FocusController has unprocessed focus events. Clearing."
                button.focusController.focusedElement.Blur();
            }

            action();
            itemTriggeredEventStream.OnNext(VoidEvent.instance);
        });
    }
}
