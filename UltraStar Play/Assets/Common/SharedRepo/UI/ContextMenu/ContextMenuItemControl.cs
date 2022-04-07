using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class ContextMenuItemControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.contextMenuLabel)]
    private Label label;

    [Inject(UxmlName = R.UxmlNames.contextMenuButton)]
    private Button button;

    private readonly string text;

    private readonly Action action;

    private readonly Subject<bool> itemTriggeredEventStream = new();
    public IObservable<bool> ItemTriggeredEventStream => itemTriggeredEventStream;

    public ContextMenuItemControl(string text, Action action)
    {
        this.text = text;
        this.action = action;
    }

    public void OnInjectionFinished()
    {
        label.text = text;
        button.RegisterCallbackButtonTriggered(() =>
        {
            if (button.focusController.focusedElement == button)
            {
                // Workaround for Unity issue "FocusController has unprocessed focus events. Clearing."
                button.focusController.focusedElement.Blur();
            }

            action();
            itemTriggeredEventStream.OnNext(true);
        });
    }
}
