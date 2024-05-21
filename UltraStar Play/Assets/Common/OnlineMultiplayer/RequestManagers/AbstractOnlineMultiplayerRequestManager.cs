using CommonOnlineMultiplayer;
using UniInject;
using UniRx;

public abstract class AbstractOnlineMultiplayerRequestManager : AbstractSingletonBehaviour, INeedInjection
{
    [Inject]
    protected OnlineMultiplayerManager onlineMultiplayerManager;

    protected override void StartSingleton()
    {
        onlineMultiplayerManager.OwnNetcodeClientStartedEventStream
            .Subscribe(_ => InitOnlineMultiplayerRequestHandlers());
    }

    protected abstract void InitOnlineMultiplayerRequestHandlers();
}
