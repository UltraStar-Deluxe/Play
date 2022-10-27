using System;
using System.Collections.Generic;
using UnityEngine;

public enum EPartyModeType
{
    Teams,
    FreeForAll
}

public enum EPartySongSelection
{
    RandomSong, // a random song is picked
    RandomSubset, // a random subset of songs is picked, and players choose which one to sing
    PlayersChoose // players choose the song
}

[Flags]
public enum EPartySongFiltering
{
    Playlist = 1 << 0,
    Artist = 1 << 1,
    Genre = 1 << 2,
    Decade = 1 << 3,
    Language = 1 << 4,
    Edition = 1 << 5,

    All = int.MaxValue,
    None = 0
}

public enum EPartyWinCondition
{
    HighestScore, // default, the highest score wins
    BestRating, // player with the best emoji rating
    HighestScoreWithAdvance, // highest score with a minimum points advance
    FirstToScore, // first player to reach N points
    LeadForNumberOfPhrases, // first player to keep the lead for N phrases
}

[Serializable]
public class WinCondition
{
    public EPartyWinCondition winType;
    public int score;
    public int phrases;

    public static implicit operator WinCondition(EPartyWinCondition winCondition) => new WinCondition() { winType = winCondition };
}

[Serializable]
public class PartyModeRound
{
    public List<SingModifier> singModifiers = new() {new SingModifier()};
    public WinCondition winCondition = EPartyWinCondition.HighestScore;
}

[Serializable]
public class PartyModeSettings
{
    public EPartyModeType mode;
    public List<string> playersList = new();
    public int playersCount = 2;
    public List<PartyModeRound> roundsList = new();
    public int roundsCount = 6;
    public List<Vector2Int> teamsList = new(); // Vector2Int is a handy serializable data structure: x = team nb, y = player nb
    public int teamsCount = 2;
    public EPartySongFiltering allSongsFiltering = EPartySongFiltering.All;
    public EPartySongSelection songSelection = EPartySongSelection.RandomSong;
    public EPartySongFiltering subsetSongFiltering = EPartySongFiltering.All;
    public int subsetSongsCount = 2;
}
