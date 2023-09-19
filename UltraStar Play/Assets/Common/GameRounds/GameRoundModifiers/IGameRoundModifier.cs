using UnityEngine.UIElements;

public interface IGameRoundModifier
{
    public string Id => GetType().Name;
    public string DisplayName { get; }
    public double DisplayOrder { get; }
    public GameRoundModifierControl CreateControl();
    public VisualElement CreateConfigurationVisualElement();
}
