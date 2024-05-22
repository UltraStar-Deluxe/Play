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

        if (CommonOnlineMultiplayerUtils.IsRemotePlayerProfile(playerProfile))
        {
            // The score is received from remote messages
            playerScoreControl.PlayerScore = new SingingResultsPlayerScore();
            disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
                GetPlayerScoreMessageName(),
                message => OnPlayerScoreMessage(message)));
        }
        else
        {
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
