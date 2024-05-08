using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/**
 * Data structure for song scores and other statistics.
 */
[Serializable]
public class Statistics
{
    /**
     * Version field to identify how the data structure needs to be loaded from file.
     * For example, when the computation of the ScoreRelevantSongHash changes,
     * then this would be a breaking change, which requires a new version.
     * When the version changes, then a mechanism to load and migrate the old version needs to be introduced.
     */
    public int Version { get; set; } = 2;
    public double TotalPlayTimeSeconds { get; set; }
    public Dictionary<string, SongStatistics> LocalStatistics { get; private set; } = new();

    // Indicates whether the Statistics have non-persisted changes.
    // The flag is checked by the StatsManager, e.g., on scene change.
    // The flag is reset by the StatsManger on save.
    [JsonIgnore]
    public bool IsDirty { get; set; }
}
