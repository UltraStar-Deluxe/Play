using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneOnlineMultiplayerControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingingResultsSceneControl singingResultsSceneControl;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    private void Start()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        // Cannot restart directly
        singingResultsSceneControl.BeforeRestartEventStream.Subscribe(evt =>
        {
            evt.cancelMessage = Translation.Get(R.Messages.onlineGame_error_notAvailable);
        });
    }
}
