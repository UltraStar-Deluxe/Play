using UnityEngine.UIElements;

public abstract class GameRoundModifier : IGameRoundModifier
{
    public string DisplayName { get; private set; }
    public virtual double DisplayOrder => 100;

    public GameRoundModifier()
    {
        string typeNameWithoutGameRoundFinishCondition = GetType().Name
            .Replace("GameRoundModifier", "")
            .Replace("RoundModifier", "")
            .Replace("Modifier", "");
        DisplayName = StringUtils.ToTitleCase(typeNameWithoutGameRoundFinishCondition);
    }

    public GameRoundModifier(string displayName)
    {
        DisplayName = displayName;
    }

    public abstract GameRoundModifierControl CreateControl();

    public abstract VisualElement CreateConfigurationVisualElement();
}
