using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public abstract class AbstractDialogControl : IDialogControl, INeedInjection
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement DialogRootVisualElement { get; protected set; }

    private readonly Subject<bool> dialogClosedEventStream = new();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    protected readonly List<IDisposable> disposables = new();
    
    public virtual void CloseDialog()
    {
        DialogRootVisualElement.RemoveFromHierarchy();
        dialogClosedEventStream.OnNext(true);
        disposables.ForEach(it => it.Dispose());
    }
}
