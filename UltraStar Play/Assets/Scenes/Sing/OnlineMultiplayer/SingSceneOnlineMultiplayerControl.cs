using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;

public class SingSceneOnlineMultiplayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    private const int SingSceneReadyResponseTimeoutInMillis = 2000;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private SingSceneData sceneData;

    private readonly List<IDisposable> disposables = new();

    // TODO: better pattern than boolean flags and try-finally to prevent reacting to own triggered events?
    private bool isFinishingSceneByOnlineMultiplayerMessage;
    private bool isAbortingSceneByOnlineMultiplayerMessage;
    private bool isPausingByOnlineMultiplayerMessage;
    private bool isUnpausingByOnlineMultiplayerMessage;

    public void OnInjectionFinished()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        if (onlineMultiplayerManager.IsHost)
        {
            SendInitialUnpauseMessageWhenAllReadyToStart(0);
        }

        // Cannot restart directly
        singSceneControl.BeforeRestartEventStream.Subscribe(evt =>
        {
            evt.cancelMessage = Translation.Get(R.Messages.onlineGame_error_notAvailable);
        });

        singSceneControl.RestartedEventStream.Subscribe(evt => SendRestartMessage());

        // Cannot skip to next lyrics
        singSceneControl.BeforeSkipEventStream.Subscribe(evt =>
        {
            evt.cancelMessage = Translation.Get(R.Messages.onlineGame_error_notAvailable);
        });
        singSceneControl.RestartedEventStream.Subscribe(evt => SendRestartMessage());

        // Send response that SingScene is ready
        disposables.Add(onlineMultiplayerManager.ObservableMessagingControl.RegisterObservedMessageHandler(
            nameof(SingSceneReadyRequestDto),
            observedMessage =>
            {
                onlineMultiplayerManager.ObservableMessagingControl.SendResponseMessage(
                    observedMessage,
                    FastBufferWriterUtils.WriteJsonValuePacked(new SingSceneReadyResponseDto()));
            }));

        // Handle messages to pause and resume the game
        disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
            nameof(PauseRequestDto),
            message =>
            {
                PauseRequestDto pauseRequestDto = FastBufferReaderUtils.ReadJsonValuePacked<PauseRequestDto>(message.MessagePayload);
                if (pauseRequestDto.ShowSenderName)
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_pausedBy,
                        "name", CommonOnlineMultiplayerUtils.GetPlayerDisplayName(onlineMultiplayerManager, message)));
                }

                try
                {
                    isPausingByOnlineMultiplayerMessage = true;
                    singSceneControl.Pause();
                }
                finally
                {
                    isPausingByOnlineMultiplayerMessage = false;
                }
            }));

        disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
            nameof(UnpauseRequestDto),
            message =>
            {
                UnpauseRequestDto unpauseRequestDto = FastBufferReaderUtils.ReadJsonValuePacked<UnpauseRequestDto>(message.MessagePayload);
                if (unpauseRequestDto.ShowSenderName)
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_resumedBy,
                        "name", CommonOnlineMultiplayerUtils.GetPlayerDisplayName(onlineMultiplayerManager, message)));
                }

                try
                {
                    isUnpausingByOnlineMultiplayerMessage = true;
                    singSceneControl.Unpause();
                }
                finally
                {
                    isUnpausingByOnlineMultiplayerMessage = false;
                }
            }));

        // Handle messages to end singing
        disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
            nameof(EndSingSceneRequest),
            message =>
            {
                try
                {
                    isFinishingSceneByOnlineMultiplayerMessage = true;
                    singSceneControl.FinishScene(false, false);
                }
                finally
                {
                    isFinishingSceneByOnlineMultiplayerMessage = false;
                }
            }));

        disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
            nameof(AbortSingSceneRequest),
            message =>
            {
                Debug.Log("Abort singing because of online multiplayer request. See log of host player for details.");
                try
                {
                    isAbortingSceneByOnlineMultiplayerMessage = true;
                    singSceneControl.AbortSceneToSongSelect();
                }
                finally
                {
                    isAbortingSceneByOnlineMultiplayerMessage = false;
                }
            }));

        disposables.Add(sceneNavigator.BeforeSceneChangeEventStream.Subscribe(evt =>
        {
            if (isFinishingSceneByOnlineMultiplayerMessage
                || isAbortingSceneByOnlineMultiplayerMessage)
            {
                return;
            }

            if (evt.NextScene is EScene.SongSelectScene)
            {
                SendAbortSingSceneMessage();
                return;
            }

            SendEndSingSceneMessage();
        }));

        // Pause / unpause
        disposables.Add(singSceneControl.PausedEventStream.Subscribe(_ =>
        {
            if (isPausingByOnlineMultiplayerMessage)
            {
                return;
            }

            SendPauseMessageToOthers();
        }));
        disposables.Add(singSceneControl.UnpausedEventStream.Subscribe(_ =>
        {
            if (isUnpausingByOnlineMultiplayerMessage)
            {
                return;
            }

            SendUnpauseMessageToOthers();
        }));
    }

    private void SendEndSingSceneMessage()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
            nameof(EndSingSceneRequest),
            FastBufferWriterUtils.WriteJsonValuePacked(new EndSingSceneRequest()),
            onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds);
    }

    private void SendAbortSingSceneMessage()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
            nameof(AbortSingSceneRequest),
            FastBufferWriterUtils.WriteJsonValuePacked(new AbortSingSceneRequest()),
            onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds);
    }

    private void SendPauseMessageToOthers()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
            nameof(PauseRequestDto),
            FastBufferWriterUtils.WriteJsonValuePacked(new PauseRequestDto()),
            onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds);
    }

    private void SendUnpauseMessageToOthers()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
            nameof(UnpauseRequestDto),
            FastBufferWriterUtils.WriteJsonValuePacked(new UnpauseRequestDto()),
            onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds);
    }

    private void SendInitialUnpauseMessageWhenAllReadyToStart(int failedAttempts)
    {
        if (!onlineMultiplayerManager.IsHost)
        {
            // Only the host sends the unpause message to all (including itself)
            // to start singing with all peers roughly at the same time.
            return;
        }

        int maxFailedAttempts = 6;

        // Send message to all lobby members to start playback when all clients are ready
        onlineMultiplayerManager.ObservableMessagingControl.SendNamedMessageToClientsAsObservable(
                nameof(SingSceneReadyRequestDto),
                FastBufferWriterUtils.WriteJsonValuePacked(new SingSceneReadyRequestDto()),
                onlineMultiplayerManager.AllLobbyMembersUnityNetcodeClientIds,
                EReliableNetworkDelivery.ReliableSequenced,
                SingSceneReadyResponseTimeoutInMillis)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to check readiness of lobby members at {failedAttempts + 1} attempt: {ex.Message}");

                if (failedAttempts >= maxFailedAttempts)
                {
                    // Failed for good, go back to song select
                    Debug.LogError($"Failed to check readiness of lobby members too many times. Going back to song select.");
                    singSceneControl.AbortSceneToSongSelect();
                }
                else
                {
                    // Try again after short delay (if this error was not triggered by a timeout already)
                    float delayInSeconds = ex is TimeoutException
                        ? 0
                        : SingSceneReadyResponseTimeoutInMillis / 1000f;
                    singSceneControl.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(delayInSeconds,
                        () => SendInitialUnpauseMessageWhenAllReadyToStart(failedAttempts + 1)));
                }
            })
            .DoOnCompleted(() =>
            {
                Debug.Log($"All Netcode clients are ready to start. Sending start message");
                onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
                    nameof(UnpauseRequestDto),
                    FastBufferWriterUtils.WriteJsonValuePacked(new UnpauseRequestDto()
                    {
                        ShowSenderName = false,
                    }),
                    onlineMultiplayerManager.AllLobbyMembersUnityNetcodeClientIds);
            })
            .Subscribe(response =>
            {
                SingSceneReadyResponseDto responseDto = FastBufferReaderUtils.ReadJsonValuePacked<SingSceneReadyResponseDto>(response.MessagePayload);
                Debug.Log($"Netcode client {response.SenderNetcodeClientId} is ready to start");
            });
    }

    private void SendRestartMessage()
    {
        if (!onlineMultiplayerManager.IsHost)
        {
            return;
        }

        SingSceneDataDto singSceneDataDto = NetcodeMessageDtoConverterUtils.ToDto(sceneData);
        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
            nameof(StartSingSceneRequestDto),
            FastBufferWriterUtils.WriteJsonValuePacked(new StartSingSceneRequestDto()
            {
                SingSceneDataDto = singSceneDataDto,
            }),
            onlineMultiplayerManager.AllLobbyMembersUnityNetcodeClientIds);
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
