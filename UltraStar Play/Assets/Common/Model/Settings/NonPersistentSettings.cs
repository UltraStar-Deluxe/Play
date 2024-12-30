using System.Collections.Generic;
using System.IO;
using CommonOnlineMultiplayer;
using UniRx;

public class NonPersistentSettings
{
    public GameRoundSettings GameRoundSettings { get; set; } = new();

    // Song select settings
    public ReactiveProperty<string> PlaylistName { get; private set; } = new("");
    public ReactiveProperty<bool> MicTestActive { get; private set; } = new();
    public Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> ActiveSearchPropertyFilters { get; private set; } = new();
    public ReactiveProperty<bool> IsShowOnlyDuetsFilterActive { get; private set; } = new();
    public ReactiveProperty<bool> IsSearchExpressionsEnabled { get; private set; } = new();
    public ReactiveProperty<string> LastValidSearchExpression { get; private set; } = new();
    public DirectoryInfo SongSelectDirectoryInfo { get; set; }
    public Dictionary<string, string> SongSelectDirectoryPathToLastSelection { get; set; } = new();

    // Song editor settings
    public ReactiveProperty<float> SongEditorMusicPlaybackSpeed { get; set; } = new(1);
    public ReactiveProperty<bool> IsSongEditorRecordingEnabled { get; set; } = new();

    // Online multiplayer related stuff
    public List<LobbyMemberPlayerProfile> LobbyMemberPlayerProfiles { get; set; } = new();
}
