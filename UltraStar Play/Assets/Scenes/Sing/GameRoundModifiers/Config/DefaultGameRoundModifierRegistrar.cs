using UnityEngine;

public class DefaultGameRoundModifierRegistrar
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        GameRoundModifierRegistry.Add<FinishOnAdvanceOfPointsReachedGameRoundModifier>();
        GameRoundModifierRegistry.Add<FinishOnPointsReachedGameRoundModifier>();
        GameRoundModifierRegistry.Add<HideLyricsGameRoundModifier>();
        GameRoundModifierRegistry.Add<HideNotesGameRoundModifier>();
        GameRoundModifierRegistry.Add<PassTheMicGameRoundModifier>();
        GameRoundModifierRegistry.Add<ReduceAudioGameRoundModifier>();
        GameRoundModifierRegistry.Add<ShortSongGameRoundModifier>();
    }
}
