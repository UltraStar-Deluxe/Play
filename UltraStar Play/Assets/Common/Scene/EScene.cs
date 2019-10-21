using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EScene
{
    // base
    MainScene = 1,
    LoadingScene = 0,
    AboutScene = 3,

    // player
    SetupPlayerScene = 7,

    // result
    HighscoreScene = 8,
    SingingResultsScene = 9,
    StatsScene = 10,

    // setting
    OptionsScene = 2,
    OptionsGameScene = 4,
    OptionsGraphicsScene = 5,
    OptionsSoundScene = 6,

    // sing
    SingScene = 11,

    // song selection
    SongSelectScene = 12,

    // song editor
    SongEditorScene = 13
}