using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EScene
{
    // base
    MainScene = 1,
    LoadingScene = 0,
    AboutScene = 3,

    // result
    HighscoreScene = 8,
    SingingResultsScene = 9,
    StatsScene = 10,

    // setting
    OptionsScene = 2,
    OptionsGameScene = 4,
    OptionsGraphicsScene = 5,
    OptionsSoundScene = 6,
    RecordingOptionsScene = 14,
    PlayerProfileSetupScene = 7,
    ContentDownloadScene = 15,
    ThemeOptionsScene = 16,
    NetworkOptionsScene = 17,
    DevelopmentOptionsScene = 18,
    CompanionAppOptionsScene = 19,
    SongLibraryOptionsScene = 20,

    // sing
    SingScene = 11,

    // song selection
    SongSelectScene = 12,

    // song editor
    SongEditorScene = 13,
}
