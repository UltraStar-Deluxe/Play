using UnityEngine.UIElements;

public class PassTheMicGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 30;

    public override GameRoundModifierControl CreateControl()
    {
        return null;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return null;
    }
}
