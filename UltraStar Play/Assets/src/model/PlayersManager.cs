using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class PlayersManager
{
    private static readonly List<Player> s_players = new List<Player>();

    static PlayersManager()
    {
        // nothing to do for now.
    }

    public static void AddPlayer(Player player)
    {
        if(player == null)
        {
            throw new UnityException("Can not add player because player is null!");
        }
        lock (s_players)
        {
            s_players.Add(player);
        }
    }

    public static void RemovePlayer(Player player)
    {
        if (player == null)
        {
            throw new UnityException("Can not add player because player is null!");
        }
        lock (s_players)
        {
            s_players.Remove(player);
        }
    }

    public static ReadOnlyCollection<Player> GetPlayers()
    {
        lock (s_players)
        {
            return s_players.AsReadOnly();
        }
    }
}
