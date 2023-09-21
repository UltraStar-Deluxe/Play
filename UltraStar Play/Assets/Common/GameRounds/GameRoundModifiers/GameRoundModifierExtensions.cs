public static class GameRoundModifierExtensions
{
    public static string GetId(this IGameRoundModifier modifier)
    {
        if (modifier == null)
        {
            return "";
        }

        return modifier.GetType().Name;
    }
}
