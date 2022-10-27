using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Singleton that handles data during a Party Mode game

public class PartyModeManager
{
    internal class PartyModeData
    {
        internal EPartyModeType mode;
        internal IReadOnlyList<string> playerNames;
        internal IReadOnlyList<(string,IReadOnlyList<string>)> teams;
        internal IReadOnlyList<PartyModeRound> rounds;
        internal int singingPlayersCount;
        internal EPartySongSelection songSelection;
        internal int songSubsetCount;
    }

    internal class RoundData
    {
        public IReadOnlyList<string> playerNames;
        public IReadOnlyList<string> allPlayerNames;
        public PartyModeRound round;
        public int number;
        public bool isLastRound;
    }

    static PartyModeManager instance;

    PartyModeData data;
    Stack<string> unusedPlayers = new ();
    HashSet<SongMeta> usedSongs = new ();
    RoundData currentRound;
    int currentRoundIndex = -1;

    internal static PartyModeData CurrentPartyData => instance.data;
    internal static RoundData CurrentRoundData => instance.currentRound;

    internal static void NewGame(PartyModeData data)
    {
        instance = new PartyModeManager { data = data };

        if (data.mode == EPartyModeType.FreeForAll)
        {
            instance.ResetUnusedPlayersList();
        }
    }

    internal static RoundData NextRoundData()
    {
        instance.currentRoundIndex++;
        if (instance.currentRoundIndex >= instance.data.rounds.Count)
        {
            throw new Exception("Trying to get next round data after last round.");
        }

        // Choose random players for the round
        var selectedPlayers = new List<string>();
        for (int i = 0; i < instance.data.singingPlayersCount; i++)
        {
            if (instance.unusedPlayers.Count == 0)
            {
                instance.ResetUnusedPlayersList(selectedPlayers);
            }
            selectedPlayers.Add(instance.unusedPlayers.Pop());
        }

        instance.currentRound = new RoundData()
        {
            allPlayerNames = instance.data.playerNames,
            playerNames = selectedPlayers,
            round = instance.data.rounds[instance.currentRoundIndex],
            number = instance.currentRoundIndex + 1,
            isLastRound = instance.currentRoundIndex == instance.data.rounds.Count - 1
        };
        return instance.currentRound;
    }

    internal static SongMeta[] GetSongMetasSubset()
    {
        // TODO filtering based on EPartySongFiltering flags

        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        List<SongMeta> allSongMetas = SongMetaManager.Instance.GetSongMetas().ToList();
        foreach (SongMeta usedSong in instance.usedSongs)
        {
            allSongMetas.Remove(usedSong);
        }

        if (instance.data.songSelection == EPartySongSelection.PlayersChoose)
        {
            return allSongMetas.ToArray();
        }

        int songCount = instance.data.songSelection == EPartySongSelection.RandomSubset ? instance.data.songSubsetCount : 1;
        var songsSubset = new List<SongMeta>();
        for (int i = 0; i < songCount; i++)
        {
            if (allSongMetas.Count == 0)
            {
                break;
            }

            SongMeta song = allSongMetas[Random.Range(0, allSongMetas.Count)];
            allSongMetas.Remove(song);
            songsSubset.Add(song);
        }
        return songsSubset.ToArray();
    }

    internal static void UseSong(SongMeta songMeta)
    {
        instance.usedSongs.Add(songMeta);
    }

    void ResetUnusedPlayersList(List<string> excludedPlayers = null)
    {
        List<string> tempList = new (data.playerNames);
        if (excludedPlayers != null)
        {
            foreach (string excludedPlayer in excludedPlayers)
            {
                tempList.Remove(excludedPlayer);
            }
        }
        ObjectUtils.ShuffleList(tempList);
        unusedPlayers = new Stack<string>(tempList);
    }
}
