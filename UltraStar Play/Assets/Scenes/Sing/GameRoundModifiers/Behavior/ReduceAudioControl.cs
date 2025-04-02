using System.Collections.Generic;
using UniInject;

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
