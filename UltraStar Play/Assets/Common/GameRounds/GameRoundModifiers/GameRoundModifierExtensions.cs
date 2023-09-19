public static class GameRoundModifierExtensions
{
    public static string GetId(this IGameRoundModifier modifier)
    {
        return modifier.GetType().Name;
    }
}
