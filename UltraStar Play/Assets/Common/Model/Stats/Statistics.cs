using System;
using System.Collections.Generic;
using FullSerializer;

/**
 * Data structure for song scores.
 */
[Serializable]
public class Statistics
{
    public double TotalPlayTimeSeconds { get; set; }
    public Dictionary<string, SongStatistics> LocalStatistics { get; private set; } = new();

    // Indicates whether the Statistics have non-persisted changes.
    // The flag is checked by the StatsManager, e.g., on scene change.
    // The flag is reset by the StatsManger on save.
    [fsIgnore]
    public bool IsDirty { get; set; }
}
