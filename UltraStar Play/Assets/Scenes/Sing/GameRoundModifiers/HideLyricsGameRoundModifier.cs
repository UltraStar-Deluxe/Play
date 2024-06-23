using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

public class HideLyricsGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 60;

    private const float AnimTimeInSeconds = 1.5f;

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

    public class HideNotesControl : ClassicConditionGameRoundModifierControl
    {
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
}
