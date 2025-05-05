using UnityEngine.UIElements;

public class ShortSongGameRoundModifier : GameRoundModifier
{
    public override double DisplayOrder => 40;

    public override GameRoundModifierControl CreateControl()
    {
        // TODO: Should create control class here. Currently, the short song is modified in SongSelectSceneControl, before starting the SingScene.
        return null;
    }

    public override VisualElement CreateConfigurationVisualElement()
    {
        return null;
    }
}
