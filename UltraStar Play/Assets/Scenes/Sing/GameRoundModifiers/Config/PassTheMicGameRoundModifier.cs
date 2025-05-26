using UnityEngine.UIElements;

public class PassTheMicGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 30;

    public override GameRoundModifierControl CreateControl()
    {
        // TODO: Should create control class here. Currently, the PassTheMicControl is created in SingSceneControl with special treatment
        return null;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return null;
    }
}
