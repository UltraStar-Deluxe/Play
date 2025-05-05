using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using Steamworks;
using Steamworks.Data;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SteamAchievementManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SteamAchievementManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SteamAchievementManager>();

    [Inject]
    private AchievementEventStream achievementEventStream;

    private readonly HashSet<AchievementId> triggeredAchievementsSinceAppStart = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        achievementEventStream
            .Subscribe(evt =>
            {
                if (evt.PlayerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile
                    && lobbyMemberPlayerProfile.IsRemote)
                {
                    // If the achievement is associated to a player, then trigger only for local players.
                    return;
                }
                TriggerAchievement(evt.AchievementId);
            })
            .AddTo(gameObject);
    }

    private void TriggerAchievement(AchievementId achievementId)
    {
        try
        {
            DoTriggerAchievement(achievementId);
        }
        catch (Exception e)
        {
            e.Log($"Failed to trigger achievement: '{achievementId.Id}'");
        }
    }

    private void DoTriggerAchievement(AchievementId achievementId)
    {
        if (triggeredAchievementsSinceAppStart.Contains(achievementId))
        {
            return;
        }
        triggeredAchievementsSinceAppStart.Add(achievementId);

        if (!TryGetAchievement(achievementId, out Achievement achievement))
        {
            Debug.LogError($"No achievement found for id: {achievementId.Id}. Maybe not connected to Steam?");
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
        if (!SteamManager.Instance.IsConnectedToSteam)
        {
            achievement = default;
            return false;
        }

        List<Achievement> achievements = SteamUserStats.Achievements.Where(achievement => achievementId.Id == achievement.Identifier).ToList();
        if (achievements.IsNullOrEmpty())
        {
            achievement = default;
            return false;
        }
        achievement = achievements[0];
        return true;
    }
}
