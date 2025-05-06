public class AchievementEvent
{
    public AchievementId AchievementId { get; private set; }
    public PlayerProfile PlayerProfile { get; private set; }

    public AchievementEvent(AchievementId achievementId, PlayerProfile playerProfile = null)
    {
        AchievementId = achievementId;
        PlayerProfile = playerProfile;
    }
}
