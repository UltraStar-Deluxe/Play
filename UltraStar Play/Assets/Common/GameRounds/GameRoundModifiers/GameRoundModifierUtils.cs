using System;
using System.Collections.Generic;
using System.Linq;

public static class GameRoundModifierUtils
{
    private static readonly List<Type> defaultGameRoundModifierTypesInAppDomain =
        ReflectionUtils.GetTypeInAppDomain<GameRoundModifier>(false);

    private static readonly List<IGameRoundModifier> defaultGameRoundModifiers =
        defaultGameRoundModifierTypesInAppDomain
            .Select(type => Activator.CreateInstance(type) as IGameRoundModifier)
            .ToList();

    public static List<IGameRoundModifier> GetGameRoundModifiers()
    {
        List<IGameRoundModifier> result = new();
        List<IGameRoundMod> modObjects = ModManager.GetModObjects<IGameRoundMod>();
        result.AddRange(defaultGameRoundModifiers);
        result.AddRange(modObjects);
        return result;
    }

    public static List<IGameRoundModifier> GetGameRoundModifiersById(IReadOnlyCollection<string> modifierIds)
    {
        if (modifierIds.IsNullOrEmpty())
        {
            return new List<IGameRoundModifier>();
        }

        return GetGameRoundModifiers()
            .Where(modifier => modifierIds.Contains(modifier.GetId()))
            .ToList();
    }

    public static List<string> GetGameRoundModifierIds(List<IGameRoundModifier> modifiers)
    {
        if (modifiers.IsNullOrEmpty())
        {
            return new List<string>();
        }

        return modifiers
            .Select(modifier => modifier.GetId())
            .ToList();
    }
}
