using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Holds all in-memory stats data
[Serializable]
public class StatsManager : MonoBehaviour
{
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
        //Update the total play time before saving
        statistics.UpdateTotalPlayTime();

        string json = JsonConverter.ToJson(Statistics, true);
        File.WriteAllText(DatabasePath(), json);
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
}
