using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

[Serializable]
public class StatisticsManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        statistics = null;
    }

    // Statistics are static to persist across scenes
    private static Statistics statistics;
    public Statistics Statistics
    {
        get
        {
            if (statistics == null)
            {
                Reload();
            }
            return statistics;
        }
    }

    public static StatisticsManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<StatisticsManager>();

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

    public void Save()
    {
        Debug.Log("Writing database");

        // Update the total play time before saving
        UpdateTotalPlayTime();

        // Do not pretty print json. The database is relatively big compared to the settings.
        // To view the JSON file, use an external viewer/formatter, for example a web browser or JSON Viewer plugin of Notepad++.
        string json = JsonConverter.ToJson(Statistics, false);
        File.WriteAllText(DatabasePath(), json);
        Statistics.IsDirty = false;
    }

    private void UpdateTotalPlayTime()
    {
        float currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        float timeSinceLastSaveInMillis = currentTimeInMillis - lastSaveTimeInMillis;
        Statistics.TotalPlayTimeSeconds += (timeSinceLastSaveInMillis / 1000f);
        lastSaveTimeInMillis = currentTimeInMillis;
    }

    public void Reload()
    {
        string databasePath = DatabasePath();
        Debug.Log("Reloading StatsManager");
        if (!File.Exists(databasePath))
        {
            Debug.LogWarning($"Database file not found. Creating new database at {databasePath}.");
            statistics = new Statistics();
            Save();
            return;
        }

        string fileContent = File.ReadAllText(databasePath);
        lastSaveTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        statistics = JsonConverter.FromJson<Statistics>(fileContent);
    }

    public string DatabasePath()
    {
        return Path.Combine(Application.persistentDataPath, "Database.json");
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
            Save();
        }
    }
}
