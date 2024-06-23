using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

public class HideNotesGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 70;

    private const float AnimTimeInSeconds = 1.5f;

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

    public class HideLyricsControl : ClassicConditionGameRoundModifierControl
    {
        public override void ActivateModifier(IReadOnlyCollection<PlayerControl> playerControls)
        {
            foreach (PlayerControl playerControl in playerControls)
            {
                playerControl.PlayerUiControl.FadeOutNotes(AnimTimeInSeconds);
            }
        }

        public override void DeactivateModifier(IReadOnlyCollection<PlayerControl> playerControls)
        {
            foreach (PlayerControl playerControl in playerControls)
            {
                playerControl.PlayerUiControl.FadeInNotes(AnimTimeInSeconds);
            }
        }
    }
}
