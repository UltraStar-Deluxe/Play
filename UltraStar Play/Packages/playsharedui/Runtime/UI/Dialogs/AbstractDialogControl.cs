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
        dialogInjectionFinishedEventStream = new();
        instantiatedDialogCount = 0;
    }

    protected static int instantiatedDialogCount;

    private static Subject<AbstractDialogControl> dialogInjectionFinishedEventStream = new();
    public static IObservable<AbstractDialogControl> DialogInjectionFinishedEventStream => dialogInjectionFinishedEventStream;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement DialogRootVisualElement { get; protected set; }

    private readonly Subject<VoidEvent> dialogClosedEventStream = new();
    public IObservable<VoidEvent> DialogClosedEventStream => dialogClosedEventStream;

    protected readonly List<IDisposable> disposables = new();

    protected AbstractDialogControl()
    {
        instantiatedDialogCount++;
        dialogInjectionFinishedEventStream.OnNext(this);
    }

    public virtual void OnInjectionFinished()
    {
        dialogInjectionFinishedEventStream.OnNext(this);
    }

    public virtual void CloseDialog()
    {
        DialogRootVisualElement.RemoveFromHierarchy();
        dialogClosedEventStream.OnNext(VoidEvent.instance);
        disposables.ForEach(it => it.Dispose());
    }
}
