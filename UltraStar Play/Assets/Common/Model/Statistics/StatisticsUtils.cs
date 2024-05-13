using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public static class StatisticsUtils
{
    public static IObservable<List<HighScoreEntry>> GetLocalHighScoreEntries(
        Statistics statistics,
        SongMeta songMeta)
    {
        SongStatistics localSongStatistics = GetLocalSongStatistics(statistics, songMeta);
        SortedSet<HighScoreEntry> highScoreEntries = localSongStatistics?.HighScoreRecord?.HighScoreEntries;
        if (highScoreEntries.IsNullOrEmpty())
        {
            return Observable.Return(new List<HighScoreEntry>());
        }

        List<HighScoreEntry> highScoreEntriesAsList = highScoreEntries.ToList();
        return Observable.Return(highScoreEntriesAsList);
    }

    public static IObservable<List<HighScoreEntry>> GetLocalAndRemoteHighScoreEntriesAllAtOnce(
        Statistics statistics,
        SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return Observable.Return(new List<HighScoreEntry>());
        }

        IObservable<HighScoreEntry> highScoreRecordObservable = GetLocalAndRemoteHighScoreRecords(statistics, songMeta)
            .SelectMany(highScoreRecord => highScoreRecord.HighScoreEntries)
            .ObserveOnMainThread();
        return ObservableUtils.AllAtOnceUntilErrorOrCompleted(highScoreRecordObservable);
    }

    private static IObservable<HighScoreRecord> GetLocalAndRemoteHighScoreRecords(
        Statistics statistics,
        SongMeta songMeta)
    {
        HighScoreRecord localHighScoreRecord = GetLocalSongStatistics(statistics, songMeta)?.HighScoreRecord;
        List<IHighScoreReader> highscoreReaders = ModManager.GetModObjects<IHighScoreReader>();
        IObservable<HighScoreRecord> localHighScoreRecordObservable = localHighScoreRecord != null
            ? Observable.Return(localHighScoreRecord)
            : Observable.Empty<HighScoreRecord>();

        IObservable<HighScoreRecord> remoteHighScoreRecordObservables = highscoreReaders
            .Select(highscoreReader => highscoreReader.ReadHighScoreRecord(songMeta))
            .Merge();

        return localHighScoreRecordObservable
            .Concat(remoteHighScoreRecordObservables)
            .ObserveOnMainThread();
    }

    public static SongStatistics GetLocalSongStatistics(Statistics statistics, SongMeta songMeta)
    {
        if (songMeta == null
            || statistics == null
            || statistics.LocalStatistics == null)
        {
            return null;
        }

        string scoreRelevantSongHash = SongIdManager.GetAndCacheScoreRelevantId(songMeta);
        statistics.LocalStatistics.TryGetValue(scoreRelevantSongHash, out SongStatistics result);
        return result;
    }

    private static HighScoreEntry GetLocalHighScoreEntry(Statistics statistics, SongMeta songMeta, EDifficulty difficulty)
    {
        if (statistics == null)
        {
            return null;
        }

        SongStatistics songStatistics = GetLocalSongStatistics(statistics, songMeta);
        if (songStatistics == null
            || songStatistics.HighScoreRecord == null
            || songStatistics.HighScoreRecord.HighScoreEntries.IsNullOrEmpty())
        {
            return null;
        }

        HighScoreEntry highScoresEntry = GetTopScores(
            songStatistics.HighScoreRecord.HighScoreEntries,
            1,
            difficulty)
            .FirstOrDefault();
        return highScoresEntry;
    }

    public static int GetLocalHighScore(Statistics statistics, SongMeta songMeta, EDifficulty difficulty)
    {
        if (statistics == null)
        {
            return 0;
        }

        HighScoreEntry highScoreEntry = GetLocalHighScoreEntry(statistics, songMeta, difficulty);
        if (highScoreEntry == null)
        {
            return 0;
        }
        return highScoreEntry.Score;
    }

    public static void RecordSongStarted(Statistics statistics, SongMeta songMeta)
    {
        if (statistics == null)
        {
            return;
        }

        Debug.Log($"Recording song started stats for '{songMeta.GetArtistDashTitle()}'");
        SongStatistics songStatistics = CreateLocalStatistics(statistics, songMeta);
        songStatistics.IncrementSongStarted();

        statistics.IsDirty = true;
    }

    public static void RecordSongFinished(
        Statistics statistics,
        SongMeta songMeta)
    {
        if (statistics == null
            || songMeta == null)
        {
            return;
        }

        Debug.Log($"Recording song finished for '{songMeta.GetArtistDashTitle()}'");
        SongStatistics songStatistics = CreateLocalStatistics(statistics, songMeta);
        songStatistics.IncrementSongFinished();
        statistics.IsDirty = true;
    }

    public static void RecordSongHighScore(
        Statistics statistics,
        SongMeta songMeta,
        List<HighScoreEntry> highScoreEntries)
    {
        RecordSongHighScoreLocally(statistics, songMeta, highScoreEntries);
        RecordSongHighScoreUsingMods(statistics, songMeta, highScoreEntries);
    }

    private static void RecordSongHighScoreLocally(
            Statistics statistics,
            SongMeta songMeta,
            List<HighScoreEntry> highScoreEntries)
        {
        if (statistics == null
            || songMeta == null
            || highScoreEntries.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"Recording high score entries for '{songMeta.GetArtistDashTitle()}'");
        SongStatistics songStatistics = CreateLocalStatistics(statistics, songMeta);
        highScoreEntries.ForEach(songStatistics.AddHighScore);
        statistics.IsDirty = true;
    }

    private static void RecordSongHighScoreUsingMods(
        Statistics statistics,
        SongMeta songMeta,
        List<HighScoreEntry> highScoreEntries)
    {
        if (statistics == null
            || songMeta == null
            || highScoreEntries.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            List<IHighScoreWriter> highScoreWriters = ModManager.GetModObjects<IHighScoreWriter>();
            if (highScoreWriters.IsNullOrEmpty())
            {
                return;
            }
            HighScoreRecord highScoreRecord = new();
            highScoreEntries.ForEach(highScoreEntry => highScoreRecord.AddRecord(highScoreEntry));
            foreach (IHighScoreWriter highScoreWriter in highScoreWriters)
            {
                try
                {
                    Debug.Log($"Recording high score entries for '{songMeta.GetArtistDashTitle()}' using mod object of type {highScoreWriter.GetType().Name}");
                    highScoreWriter.WriteHighScoreRecord(highScoreRecord, songMeta);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError(
                        $"Failed to write high score using mod object of type {highScoreWriter.GetType().Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to write high scores using mods");
        }
    }

    private static SongStatistics CreateLocalStatistics(Statistics statistics, SongMeta songMeta)
    {
        if (statistics == null
            || statistics.LocalStatistics == null)
        {
            return null;
        }

        string scoreRelevantSongHash = SongIdManager.GetAndCacheScoreRelevantId(songMeta);
        SongStatistics songStatistics = statistics.LocalStatistics.GetOrInitialize(scoreRelevantSongHash);
        songStatistics.SongArtist = songMeta.Artist;
        songStatistics.SongTitle = songMeta.Title;
        return songStatistics;
    }

    public static bool HasHighscore(Statistics statistics, SongMeta songMeta)
    {
        if (statistics == null)
        {
            return false;
        }

        SongStatistics localStatistics = GetLocalSongStatistics(statistics, songMeta);
        return localStatistics != null
            && localStatistics.HighScoreRecord != null
            && localStatistics.HighScoreRecord.HighScoreEntries != null
            && localStatistics.HighScoreRecord.HighScoreEntries.Count > 0;
    }

    public static List<HighScoreEntry> GetTopScores(IReadOnlyCollection<HighScoreEntry> highScoreEntries, int count, EDifficulty difficulty)
    {
        if (highScoreEntries.IsNullOrEmpty())
        {
            return new List<HighScoreEntry>();
        }
        return highScoreEntries
            .Where(it => it.Difficulty == difficulty)
            .OrderBy(it => -it.Score)
            .Take(count)
            .ToList();
    }
}
