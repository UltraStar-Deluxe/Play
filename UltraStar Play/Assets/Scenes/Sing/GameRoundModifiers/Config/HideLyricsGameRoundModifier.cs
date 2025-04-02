using UnityEngine.UIElements;

public class HideLyricsGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 60;

    private readonly ClassicGameRoundModifierConditionSettings conditionSettings = new();

    public override GameRoundModifierControl CreateControl()
    {
        HideLyricsControl modifierControl = GameObjectUtils
            .CreateGameObjectWithComponent<HideLyricsControl>();
        modifierControl.conditionSettings = conditionSettings;
        return modifierControl;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return conditionSettings.CreateConfigurationVisualElement();
    }
}
