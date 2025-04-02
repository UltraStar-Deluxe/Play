using UnityEngine.UIElements;

public class HideNotesGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 70;

    private readonly ClassicGameRoundModifierConditionSettings conditionSettings = new();

    public override GameRoundModifierControl CreateControl()
    {
        HideNotesControl modifierControl = GameObjectUtils
            .CreateGameObjectWithComponent<HideNotesControl>();
        modifierControl.conditionSettings = conditionSettings;
        return modifierControl;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return conditionSettings.CreateConfigurationVisualElement();
    }
}
