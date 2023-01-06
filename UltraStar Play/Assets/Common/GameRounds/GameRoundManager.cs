using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameRoundManager : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        gameRoundDatas.Clear();
    }

    private static readonly List<GameRoundData> gameRoundDatas = new();

    public static GameRoundManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<GameRoundManager>("GameRoundManager");
        }
    }

    public void AddGameRound(GameRoundData gameRoundData)
    {
        gameRoundDatas.Add(gameRoundData);
    }

    public void RemoveGameRound(GameRoundData gameRoundData)
    {
        gameRoundDatas.Remove(gameRoundData);
    }

    public IReadOnlyList<GameRoundData> GetGameRoundDatas()
    {
        return gameRoundDatas;
    }
}
