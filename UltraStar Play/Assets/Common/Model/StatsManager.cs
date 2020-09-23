using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Holds all in-memory stats data
[Serializable]
public class StatsManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
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

    public static StatsManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<StatsManager>("StatsManager");
        }
    }

    public void Save()
    {
        Debug.Log("Writing database");
        // Update the total play time before saving
        Statistics.UpdateTotalPlayTime();

        // Do not pretty print json. The database is relatively big compared to the settings.
        // To view the JSON file, use an external viewer/formatter, for example a web browser or JSON Viewer plugin of Notepad++.
        string json = JsonConverter.ToJson(Statistics, false);
        File.WriteAllText(DatabasePath(), json);
        Statistics.IsDirty = false;
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
        statistics = JsonConverter.FromJson<Statistics>(fileContent);
    }

    public string DatabasePath()
    {
        return Path.Combine(Application.persistentDataPath, "Database.json");
    }

    void OnDisable()
    {
        // Save the statistics when necessary.
        if (statistics != null && statistics.IsDirty)
        {
            Debug.Log("Stats have changed. Saving stats");
            Save();
        }
    }
}
