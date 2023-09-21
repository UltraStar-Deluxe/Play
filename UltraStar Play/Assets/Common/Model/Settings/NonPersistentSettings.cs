using System.Collections.Generic;
using System.IO;
using UniRx;

public class NonPersistentSettings
{
    public GameRoundSettings GameRoundSettings { get; set; } = new();

    // Song select settings
    public ReactiveProperty<string> PlaylistName { get; private set; } = new("");
    public ReactiveProperty<bool> MicTestActive { get; private set; } = new();
    public Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> ActiveSearchPropertyFilters { get; private set; } = new();
    public ReactiveProperty<bool> IsShowOnlyDuetsFilterActive { get; private set; } = new();
    public ReactiveProperty<bool> IsShowOnlyFilesWithoutSingAlongDataFilterActive { get; private set; } = new();
    public Dictionary<string, MicProfileReference> PlayerProfileNameToLastUsedMicProfile { get; private set; } = new();
    public DirectoryInfo SongSelectDirectoryInfo { get; set; }

    // Song editor settings
    public ReactiveProperty<float> SongEditorMusicPlaybackSpeed { get; set; } = new(1);
    public ReactiveProperty<bool> IsSongEditorRecordingEnabled { get; set; } = new();
}
