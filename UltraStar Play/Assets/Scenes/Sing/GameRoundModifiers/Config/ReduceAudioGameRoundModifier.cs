using UnityEngine.UIElements;

public class ReduceAudioGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 50;

    private readonly ClassicGameRoundModifierConditionSettings conditionSettings = new();

    public override GameRoundModifierControl CreateControl()
    {
        ReduceAudioControl modifierControl = GameObjectUtils
            .CreateGameObjectWithComponent<ReduceAudioControl>();
        modifierControl.conditionSettings = conditionSettings;
        return modifierControl;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return conditionSettings.CreateConfigurationVisualElement();
    }
}
