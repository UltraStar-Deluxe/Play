using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameRoundModifierRegistry
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        defaultGameRoundModifiers = new();
    }
    private static List<IGameRoundModifier> defaultGameRoundModifiers = new();

    public static List<IGameRoundModifier> GetAll()
    {
        List<IGameRoundModifier> result = new();
        List<IGameRoundMod> modObjects = ModManager.GetModObjects<IGameRoundMod>();
        result.AddRange(defaultGameRoundModifiers);
        result.AddRange(modObjects);
        return result;
    }

    public static List<IGameRoundModifier> GetAllById(IReadOnlyCollection<string> modifierIds)
    {
        if (modifierIds.IsNullOrEmpty())
        {
            return new List<IGameRoundModifier>();
        }

        return GetAll()
            .Where(modifier => modifierIds.Contains(modifier.GetId()))
            .ToList();
    }

    public static void Add<T>() where T : IGameRoundModifier, new()
    {
        if (defaultGameRoundModifiers.AnyMatch(it => it is T))
        {
            throw new InvalidOperationException($"GameRoundModifier already registered: type '{typeof(T).Name}'");
        }

        defaultGameRoundModifiers.Add(new T());
    }
}
