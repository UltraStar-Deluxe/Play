using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class FileStatisticsLoaderSaver : IStatisticsLoaderSaver
{
    private string statisticsPath;
    private string StatisticsPath
    {
        get
        {
            if (statisticsPath.IsNullOrEmpty())
            {
                statisticsPath = ApplicationUtils.GetPersistentDataPath("Database.json");
            }

            return statisticsPath;
        }
    }

    public Statistics LoadStatistics()
    {
        Debug.Log($"Loading statistics from '{StatisticsPath}'");

        string databasePath = StatisticsPath;
        if (!File.Exists(databasePath))
        {
            Debug.LogWarning($"Database file not found. Creating default database at {databasePath}.");
            SaveStatistics(DefaultStatisticsFactory.CreateDefaultStatistics());
        }

        string fileContent = File.ReadAllText(databasePath);
        try
        {
            return JsonConverter.FromJson<Statistics>(fileContent);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            // Create copy of original settings file with timestamp
            string originalPath = StatisticsPath;
            string dateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd'T'HH-mm-ss", CultureInfo.InvariantCulture);
            string copyPath = originalPath.Replace(".json", $"_crashed_{dateTimeStamp}.json");
            File.WriteAllText(copyPath, fileContent);
            Debug.LogError($"Failed to load statistics from file. Using new default instance instead. You can find the original file in '{copyPath}'. Original file content: {fileContent}");

            // Fall back to default statistics
            return DefaultStatisticsFactory.CreateDefaultStatistics();
        }
    }

    public void SaveStatistics(Statistics statistics)
    {
        // Do not pretty print json. The database is relatively big compared to the settings.
        // To view the JSON file, use an external viewer/formatter, for example a web browser or JSON Viewer plugin of Notepad++.
        string json = JsonConverter.ToJson(statistics, false);
        File.WriteAllText(StatisticsPath, json);
    }
}
