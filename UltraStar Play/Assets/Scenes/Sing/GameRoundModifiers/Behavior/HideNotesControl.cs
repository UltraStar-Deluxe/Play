using System.Collections.Generic;

public class HideNotesControl : ClassicConditionGameRoundModifierControl
{
    private const float AnimTimeInSeconds = 1.5f;

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
