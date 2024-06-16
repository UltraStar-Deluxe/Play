using UnityEngine.UIElements;

public class ShortSongGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 40;

    public override GameRoundModifierControl CreateControl()
    {
        return null;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return null;
    }
}
