using CommonOnlineMultiplayer;
using UniInject;
using UniRx;

public class PlayerScoreNetworkControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private PlayerProfile playerProfile;

    [Inject]
    private PlayerScoreControl playerScoreControl;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    private PlayerScoreNetworkBehavior playerScoreNetworkBehavior;

    public void OnInjectionFinished()
    {
        if (playerProfile != onlineMultiplayerManager.OwnLobbyMemberPlayerProfile)
        {
            return;
        }

        playerScoreNetworkBehavior = onlineMultiplayerManager.AddNetworkBehaviourToOwnLobbyMemberIfMissing<PlayerScoreNetworkBehavior>();

        playerScoreControl.SentenceScoreEventStream
            .Subscribe(_ => OnSentenceScoreEvent());
    }

    private void OnSentenceScoreEvent()
    {
        playerScoreNetworkBehavior.totalScore.Value = playerScoreControl.TotalScore;
    }
}
