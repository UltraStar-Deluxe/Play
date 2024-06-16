using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;

public class PlayerScoreOnlineMultiplayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    [Inject]
    private PlayerScoreControl playerScoreControl;

    [Inject]
    private PlayerProfile playerProfile;

    private readonly List<IDisposable> disposables = new();

    public void OnInjectionFinished()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        // Score is updated only at the end of sentences.
        // This is done for remote players via messages,
        // and synthetically for local players to keep scores synchronized.
        playerScoreControl.PlayerScore = new SingingResultsPlayerScore();

        if (CommonOnlineMultiplayerUtils.IsRemotePlayerProfile(playerProfile))
        {
            // The score is received from remote messages
            disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
                GetPlayerScoreMessageName(),
                message => OnPlayerScoreMessage(message)));
        }
        else
        {
            // Update local score
            playerScoreControl.ScoreCalculatedEventStream.Subscribe(evt =>
            {
                playerScoreControl.PlayerScore = new SingingResultsPlayerScore(playerScoreControl.CalculationData);
            });

            // Send score to remote clients
            playerScoreControl.ScoreChangedEventStream.Subscribe(evt =>
            {
                SendPlayerScoreMessageToOtherLobbyMembers();
            });
        }
    }

    private void SendPlayerScoreMessageToOtherLobbyMembers()
    {
        SingingResultsPlayerScoreRequestDto playerScoreRequestDto = new()
        {
            SingingResultsPlayerScore = new SingingResultsPlayerScore(playerScoreControl.PlayerScore),
        };

        Log.Debug(() => $"Sending score to other lobby members: {GetPlayerScoreMessageName()} - {JsonConverter.ToJson(playerScoreRequestDto)}");
        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
            GetPlayerScoreMessageName(),
            FastBufferWriterUtils.WriteJsonValuePacked(playerScoreRequestDto),
            onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds);
    }

    private string GetPlayerScoreMessageName()
    {
        if (playerProfile is not LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            throw new IllegalStateException("Failed to construct online multiplayer message name because player is not a lobby member.");
        }

        return $"{nameof(SingingResultsPlayerScoreRequestDto)}-{lobbyMemberPlayerProfile.Name}-{lobbyMemberPlayerProfile.UnityNetcodeClientId}";
    }

    private void OnPlayerScoreMessage(NamedMessage message)
    {
        SingingResultsPlayerScoreRequestDto requestDto = FastBufferReaderUtils.ReadJsonValuePacked<SingingResultsPlayerScoreRequestDto>(message.MessagePayload);
        playerScoreControl.PlayerScore = requestDto.SingingResultsPlayerScore;
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
