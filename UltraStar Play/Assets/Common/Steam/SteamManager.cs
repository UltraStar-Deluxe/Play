using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SteamManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SteamManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamManager>();
    
    private const int MelodyManiaSteamAppId = 2394070;

    public bool IsConnectedToSteam { get; private set; }
    public SteamId PlayerSteamId { get; private set; }
    public string PlayerName { get; private set; } = "";

    [Inject]
    private AchievementEventStream achievementEventStream;
    
    public List<Achievement> Achievements => SteamUserStats.Achievements.ToList();
    
    private Subject<bool> connectedToSteamEventStream = new Subject<bool>();
    public IObservable<bool> ConnectedToSteamEventStream => connectedToSteamEventStream;

    private Subject<bool> disconnectedFromSteamEventStream = new Subject<bool>();
    public IObservable<bool> DisconnectedFromSteamEventStream => disconnectedFromSteamEventStream;

    private string playerSteamIdString;
    private List<Lobby> activeUnrankedLobbies;
    private List<Lobby> activeRankedLobbies;
    
    private HashSet<AchievementId> triggeredAchievementsSinceAppStart = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void OnEnableSingleton()
    {
        // SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
        // SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
        // SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
        // SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
        // SteamMatchmaking.OnChatMessage += OnChatMessageCallback;
        // SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
        // SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
        // SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequestedCallback;
        // SteamApps.OnDlcInstalled += OnDlcInstalledCallback;
    }

    protected override void OnDisableSingleton()
    {
        // SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreatedCallback;
        // SteamMatchmaking.OnLobbyCreated -= OnLobbyCreatedCallback;
        // SteamMatchmaking.OnLobbyEntered -= OnLobbyEnteredCallback;
        // SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoinedCallback;
        // SteamMatchmaking.OnChatMessage -= OnChatMessageCallback;
        // SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnectedCallback;
        // SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeaveCallback;
        // SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequestedCallback;
        // SteamApps.OnDlcInstalled -= OnDlcInstalledCallback;
    }

    protected override void StartSingleton()
    {
        achievementEventStream.Subscribe(achievementId => TriggerAchievement(achievementId));
        InitSteamClient();
    }

    private void InitSteamClient()
    {
        try
        {
            Debug.Log("Initializing SteamClient");
            SteamClient.Init(MelodyManiaSteamAppId);
            if (!SteamClient.IsValid)
            {
                throw new SteamException("Steam client not valid");
            }
            
            PlayerName = SteamClient.Name;
            PlayerSteamId = SteamClient.SteamId;
            playerSteamIdString = PlayerSteamId.ToString();
            activeUnrankedLobbies = new List<Lobby>();
            activeRankedLobbies = new List<Lobby>();
            IsConnectedToSteam = true;
            
            connectedToSteamEventStream.OnNext(true);
            Debug.Log("Steam successfully initialized, PlayerName: " + PlayerName);
        }
        catch (Exception e)
        {
            IsConnectedToSteam = false;
            playerSteamIdString = "NoSteamId";
            Debug.LogException(e);
        }
    }

    private void Update()
    {
        if (this != Instance)
        {
            return;
        }

        SteamClient.RunCallbacks();
    }

    protected override void OnDestroySingleton()
    {
        Debug.Log("Shutting down SteamClient...");
        SteamClient.Shutdown();
        Debug.Log("SteamClient shut down successfully");
        disconnectedFromSteamEventStream.OnNext(true);
    }

    private void TriggerAchievement(AchievementId achievementId)
    {
        if (triggeredAchievementsSinceAppStart.Contains(achievementId))
        {
            return;
        }
        triggeredAchievementsSinceAppStart.Add(achievementId);
        
        if (!IsConnectedToSteam)
        {
            // Maybe next time
            Debug.LogWarning($"Attempt to trigger {achievementId}, but not connected to SteamClient");
            return;
        }
        
        Achievement achievement = new(achievementId.Id);
        achievement.Trigger();
    }
}
