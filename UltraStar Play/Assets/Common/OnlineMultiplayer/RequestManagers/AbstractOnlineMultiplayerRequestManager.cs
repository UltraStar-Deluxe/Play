using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;

public abstract class AbstractOnlineMultiplayerRequestManager : AbstractSingletonBehaviour, INeedInjection
{
    [Inject]
    protected OnlineMultiplayerManager onlineMultiplayerManager;

    private readonly List<IDisposable> disposables = new();

    protected override void StartSingleton()
    {
        disposables.Add(onlineMultiplayerManager.OwnNetcodeClientStartedEventStream
            .Subscribe(_ => InitOnlineMultiplayerRequestHandlers()));
    }

    protected override void OnDestroySingleton()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
    }

    protected abstract void InitOnlineMultiplayerRequestHandlers();
}
