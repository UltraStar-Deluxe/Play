using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EScreen
{
    // base
    SMainView = 1,
    SLoadingView = 0,
    SAboutView = 3,

    // player
    SSetupPlayerView = 7,

    // result
    SHighscoreView = 8,
    SResultSongView = 9,
    SStatsView = 10,

    // setting
    SOptionsView = 2,
    SOptionsGameView = 4,
    SOptionsGraphicsView = 5,
    SOptionsSoundView = 6,

    // sing
    SSingView = 11,
    SJukeboxView = 15,

    // songSelection
    SSongFolderView = 12,
    SSongPlaylistView = 13,
    SSongSelectView = 14

}