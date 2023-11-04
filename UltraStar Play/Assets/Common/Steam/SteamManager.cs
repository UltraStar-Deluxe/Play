using System;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SteamManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SteamManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamManager>();

    public bool IsConnectedToSteam { get; private set; }
    public SteamId PlayerSteamId { get; private set; }
    public string PlayerName { get; private set; } = "Missing Name";

    [Inject]
    private AchievementEventStream achievementEventStream;

    [Inject]
    private FacepunchTransport facepunchTransport;

    private readonly Dictionary<string, Achievement> achievementIdToAchievement = new();
    private readonly Subject<bool> connectedToSteamEventStream = new();
    public IObservable<bool> ConnectedToSteamEventStream => connectedToSteamEventStream;

    private readonly Subject<bool> disconnectedFromSteamEventStream = new();
    public IObservable<bool> DisconnectedFromSteamEventStream => disconnectedFromSteamEventStream;

    private string playerSteamIdString;
    private List<Lobby> activeUnrankedLobbies;
    private List<Lobby> activeRankedLobbies;

    private readonly HashSet<AchievementId> triggeredAchievementsSinceAppStart = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        achievementEventStream
            .Subscribe(achievementId => TriggerAchievement(achievementId))
            .AddTo(gameObject);
        InitSteamClient();
    }

    private void InitSteamClient()
    {
        try
        {
            if (IsConnectedToSteam)
            {
                Debug.LogWarning("Already connected to Steam");
                return;
            }

            // SteamClient is initialized in FacepunchTransport.Awake()
            if (!SteamClient.IsLoggedOn)
            {
                throw new SteamException("SteamClient.IsLoggedOn is false");
            }

            IsConnectedToSteam = true;
            PlayerName = SteamClient.Name;
            PlayerSteamId = SteamClient.SteamId;
            playerSteamIdString = PlayerSteamId.ToString();
            activeUnrankedLobbies = new List<Lobby>();
            activeRankedLobbies = new List<Lobby>();
            IsConnectedToSteam = true;

            bool requestCurrentStatsSuccess = SteamUserStats.RequestCurrentStats();
            if (!requestCurrentStatsSuccess)
            {
                Debug.LogError("Connected to Steam but failed to request current stats");
            }

            achievementIdToAchievement.Clear();
            SteamUserStats.Achievements.ForEach(achievement => achievementIdToAchievement[achievement.Identifier] = achievement);

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

        if (!TryGetAchievement(achievementId, out Achievement achievement))
        {
            Debug.LogError($"No achievement found for id: {achievementId.Id}");
            return;
        }

        if (achievement.State)
        {
            Debug.Log($"Skipping already unlocked achievement {achievementId.Id}");
            return;
        }

        try
        {
            Debug.Log("Unlocking achievement: " + achievementId.Id);
            bool success = achievement.Trigger();
            if (!success)
            {
                Debug.LogWarning($"Failed to unlock achievement: {achievementId.Id}");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private bool TryGetAchievement(AchievementId achievementId, out Achievement achievement)
    {
        return achievementIdToAchievement.TryGetValue(achievementId.Id, out achievement);
    }
}
