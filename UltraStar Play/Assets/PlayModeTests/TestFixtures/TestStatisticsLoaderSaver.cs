using UnityEngine;

public class TestStatisticsLoaderSaver : IStatisticsLoaderSaver
{
    public Statistics LoadStatistics()
    {
        Debug.Log("Returning new test statistics");
        return new TestStatistics();
    }

    public void SaveStatistics(Statistics statistics)
    {
        Debug.Log("Not saving test statistics.");
    }
}
