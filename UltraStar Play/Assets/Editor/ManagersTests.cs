using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class ManagersTests
{
    [Test]
    public void TestPlayersManagerCreation()
    {
        PlayersManager.ClearPlayers();
        Assert.IsTrue(PlayersManager.GetPlayers().Count == 0);
    }

    [Test]
    public void TestPlayersManagerAddPlayer()
    {
        PlayersManager.ClearPlayers();
        Player player = new Player();
        PlayersManager.AddPlayer(player);
        System.Collections.ObjectModel.ReadOnlyCollection<Player> players = PlayersManager.GetPlayers();
        Assert.AreEqual(1, players.Count);
        Assert.AreEqual(player, players[0]);
    }

    [Test]
    public void TestPlayersManagerRemovePlayer()
    {
        PlayersManager.ClearPlayers();
        Player player1 = new Player();
        Player player2 = new Player();
        Player player3 = new Player();
        PlayersManager.AddPlayer(player1);
        PlayersManager.AddPlayer(player2);
        PlayersManager.AddPlayer(player3);
        PlayersManager.RemovePlayer(player2);
        System.Collections.ObjectModel.ReadOnlyCollection<Player> players = PlayersManager.GetPlayers();
        Assert.AreEqual(2, players.Count);
        Assert.AreEqual(player1, players[0]);
        Assert.AreEqual(player3, players[1]);
    }

    [Test]
    public void TestSettingsManagerGetSetting()
    {
        SettingsManager.Reload();
        Assert.IsTrue((bool) SettingsManager.GetSetting(ESetting.FullScreen));
    }

    [Test]
    public void TestSettingsManagerSetSetting()
    {
        SettingsManager.Reload();
        bool isFullscreen = (bool)SettingsManager.GetSetting(ESetting.FullScreen);
        SettingsManager.SetSetting(ESetting.FullScreen, false);
        Assert.IsFalse((bool)SettingsManager.GetSetting(ESetting.FullScreen));
    }
}
