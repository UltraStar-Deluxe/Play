using System;
using Steamworks;
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
    public string PlayerName { get; private set; } = "Player";

    [Inject]
    private SteamAchievementManager steamAchievementManager;

    [Inject]
    private SteamWorkshopManager steamWorkshopManager;

    private readonly Subject<VoidEvent> connectedToSteamEventStream = new();
    public IObservable<VoidEvent> ConnectedToSteamEventStream => connectedToSteamEventStream;

    private readonly Subject<VoidEvent> disconnectedFromSteamEventStream = new();
    public IObservable<VoidEvent> DisconnectedFromSteamEventStream => disconnectedFromSteamEventStream;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
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

            PlayerName = SteamClient.Name;
            PlayerSteamId = SteamClient.SteamId;
            IsConnectedToSteam = true;

            bool requestCurrentStatsSuccess = SteamUserStats.RequestCurrentStats();
            if (!requestCurrentStatsSuccess)
            {
                Debug.LogError("Connected to Steam but failed to request current stats");
            }

            steamAchievementManager.SetAvailableAchievements(SteamUserStats.Achievements);

            steamWorkshopManager.DownloadWorkshopItems();

            connectedToSteamEventStream.OnNext(VoidEvent.instance);
            Debug.Log($"Steam successfully initialized: PlayerName: {PlayerName}, SteamUser.VoiceRecord: {SteamUser.VoiceRecord}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to initialize Steam, maybe not connected to Steam client: {e.Message}");
            IsConnectedToSteam = false;
        }
    }

    protected override void OnDestroySingleton()
    {
        Debug.Log("Shutting down SteamClient...");
        SteamClient.Shutdown();
        Debug.Log("SteamClient shut down successfully");
        disconnectedFromSteamEventStream.OnNext(VoidEvent.instance);
    }
}
