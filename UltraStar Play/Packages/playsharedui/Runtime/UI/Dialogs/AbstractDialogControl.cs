using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractDialogControl : IDialogControl, INeedInjection, IInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        instantiatedDialogCount = 0;
    }

    protected static int instantiatedDialogCount;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement DialogRootVisualElement { get; protected set; }

    private readonly Subject<VoidEvent> dialogClosedEventStream = new();
    public IObservable<VoidEvent> DialogClosedEventStream => dialogClosedEventStream;

    protected readonly List<IDisposable> disposables = new();

    protected AbstractDialogControl()
    {
        instantiatedDialogCount++;
    }

    public virtual void OnInjectionFinished()
    {
    }

    public virtual void CloseDialog()
    {
        DialogRootVisualElement.RemoveFromHierarchy();
        dialogClosedEventStream.OnNext(VoidEvent.instance);
        disposables.ForEach(it => it.Dispose());
    }
}
