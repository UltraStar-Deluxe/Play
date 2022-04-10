using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public abstract class AbstractDialogControl : IDialogControl, INeedInjection
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    protected VisualElement dialogRootVisualElement;

    private readonly Subject<bool> dialogClosedEventStream = new();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    public virtual void CloseDialog()
    {
        dialogRootVisualElement.RemoveFromHierarchy();
        dialogClosedEventStream.OnNext(true);
    }
}
