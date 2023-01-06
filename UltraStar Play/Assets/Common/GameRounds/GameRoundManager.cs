using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    public bool HasGameRounds => !GetGameRounds().IsNullOrEmpty();

    private readonly Subject<GameRoundsChangedEvent> gameRoundsChangedEventStream = new();
    public IObservable<GameRoundsChangedEvent> GameRoundsChangedEventStream => gameRoundsChangedEventStream;

    [Inject]
    private UiManager uiManager;

    public void AddGameRound(GameRoundData gameRound)
    {
        gameRoundDatas.Add(gameRound);
        gameRoundsChangedEventStream.OnNext(new GameRoundsChangedEvent(gameRound));
    }

    public void RemoveGameRound(GameRoundData gameRound)
    {
        gameRoundDatas.Remove(gameRound);
        gameRoundsChangedEventStream.OnNext(new GameRoundsChangedEvent(gameRound));
    }

    public IReadOnlyList<GameRoundData> GetGameRounds()
    {
        return gameRoundDatas;
    }

    public void DeleteNewestSongFromGameRound(GameRoundData gameRound)
    {
        if (gameRound.SongMetas.IsNullOrEmpty())
        {
            return;
        }

        gameRound.SongMetas.RemoveAt(gameRound.SongMetas.Count - 1);
        if (gameRound.SongMetas.IsNullOrEmpty())
        {
            // Remove the game round completely
            RemoveGameRound(gameRound);
            return;
        }

        gameRoundsChangedEventStream.OnNext(new GameRoundsChangedEvent(gameRound));
    }

    public void AddSongToLastGameRound(SongMeta songMeta)
    {
        GameRoundData lastGameRound = gameRoundDatas.LastOrDefault();
        if (lastGameRound == null)
        {
            return;
        }

        if (lastGameRound.SongMetas.Contains(songMeta))
        {
            uiManager.CreateNotificationVisualElement("Song is already in list");
            return;
        }

        lastGameRound.SongMetas.Add(songMeta);
        gameRoundsChangedEventStream.OnNext(new GameRoundsChangedEvent(lastGameRound));
    }
}
