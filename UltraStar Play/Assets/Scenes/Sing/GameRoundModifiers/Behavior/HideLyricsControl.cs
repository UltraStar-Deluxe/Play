using System.Collections.Generic;

public class HideLyricsControl : ClassicConditionGameRoundModifierControl
{
    private const float AnimTimeInSeconds = 1.5f;

    public override void ActivateModifier(IReadOnlyCollection<PlayerControl> playerControls)
    {
        foreach (PlayerControl playerControl in playerControls)
        {
            singSceneControl.FadeOutLyrics(playerControl.Voice, AnimTimeInSeconds);
            playerControl.PlayerUiControl.NoteDisplayer.FadeOutLyricsOnNotes(AnimTimeInSeconds);
        }
    }

    public override void DeactivateModifier(IReadOnlyCollection<PlayerControl> playerControls)
    {
        foreach (PlayerControl playerControl in playerControls)
        {
            singSceneControl.FadeInLyrics(playerControl.Voice, AnimTimeInSeconds);
            playerControl.PlayerUiControl.NoteDisplayer.FadeInLyricsOnNotes(AnimTimeInSeconds);
        }
    }
}
