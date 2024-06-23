using System.Collections.Generic;
using UniInject;
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

    public class ReduceAudioControl : ClassicConditionGameRoundModifierControl
    {
        [Inject]
        private Settings settings;

        private readonly HashSet<PlayerControl> playerControlsWithActiveModifier = new();

        public override void ActivateModifier(IReadOnlyCollection<PlayerControl> playerControls)
        {
            playerControlsWithActiveModifier.AddRange(playerControls);
            UpdateVolume();
        }

        public override void DeactivateModifier(IReadOnlyCollection<PlayerControl> playerControls)
        {
            playerControlsWithActiveModifier.RemoveRange(playerControls);
            UpdateVolume();
        }

        private void UpdateVolume()
        {
            if (playerControlsWithActiveModifier.IsNullOrEmpty())
            {
                singSceneControl.ModifiedVolumePercent.Value = 100;
            }
            else
            {
                singSceneControl.ModifiedVolumePercent.Value = settings.ReducedAudioVolumePercent;
            }
        }
    }
}
