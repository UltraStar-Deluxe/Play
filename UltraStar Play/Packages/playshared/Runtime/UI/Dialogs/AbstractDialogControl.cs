using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public abstract class AbstractDialogControl : IDialogControl, INeedInjection
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement DialogRootVisualElement { get; protected set; }

    private readonly Subject<bool> dialogClosedEventStream = new();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    public virtual void CloseDialog()
    {
        DialogRootVisualElement.RemoveFromHierarchy();
        dialogClosedEventStream.OnNext(true);
    }
}
