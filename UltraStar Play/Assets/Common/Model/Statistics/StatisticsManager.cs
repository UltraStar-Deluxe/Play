using System;
using UniInject;
using UniRx;
using UnityEngine;

[Serializable]
public class StatisticsManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        StatisticsLoaderSaver = null;
    }

    public static StatisticsManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<StatisticsManager>();

    public static IStatisticsLoaderSaver StatisticsLoaderSaver { get; set; }

    private Statistics statistics;
    public Statistics Statistics
    {
        get
        {
            if (Instance != this)
            {
                throw new InvalidOperationException("Statistics can only be accessed from the singleton instance");
            }

            if (statistics == null)
            {
                LoadStatistics();
            }
            return statistics;
        }
    }

    private float lastSaveTimeInMillis;

    [Inject]
    private SceneNavigator sceneNavigator;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        sceneNavigator.SceneChangedEventStream
            .Subscribe(_ => SaveStatsIfDirty());
    }

    private void UpdateTotalPlayTime()
    {
        float currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        float timeSinceLastSaveInMillis = currentTimeInMillis - lastSaveTimeInMillis;
        Statistics.TotalPlayTimeSeconds += (timeSinceLastSaveInMillis / 1000f);
        lastSaveTimeInMillis = currentTimeInMillis;
    }

    private void InitStatisticsLoaderSaverIfNotDoneYet()
    {
        if (StatisticsLoaderSaver != null)
        {
            return;
        }
        StatisticsLoaderSaver = new FileStatisticsLoaderSaver();
        Debug.Log($"No {nameof(StatisticsLoaderSaver)} set. Using new instance of {StatisticsLoaderSaver.GetType()}.");
    }

    public void SaveStatistics()
    {
        if (statistics == null)
        {
            Debug.LogWarning("Failed to save statistics. Statistics are null.");
            return;
        }

        InitStatisticsLoaderSaverIfNotDoneYet();

        // Update the total play time before saving
        UpdateTotalPlayTime();

        StatisticsLoaderSaver.SaveStatistics(statistics);

        Statistics.IsDirty = false;
    }

    private void LoadStatistics()
    {
        if (statistics != null)
        {
            throw new InvalidOperationException("Statistics have been loaded already");
        }
        InitStatisticsLoaderSaverIfNotDoneYet();

        statistics = StatisticsLoaderSaver.LoadStatistics();
        lastSaveTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
    }

    protected override void OnDisableSingleton()
    {
        SaveStatsIfDirty();
    }

    private void SaveStatsIfDirty()
    {
        if (statistics != null
            && statistics.IsDirty)
        {
            Debug.Log("Stats have changed, saving.");
            SaveStatistics();
        }
    }
}
