using UnityEngine.UIElements;

public interface IGameRoundModifier
{
    public string DisplayName { get; }
    public double DisplayOrder { get; }
    public GameRoundModifierControl CreateControl();
    public VisualElement CreateConfigurationVisualElement();
}
