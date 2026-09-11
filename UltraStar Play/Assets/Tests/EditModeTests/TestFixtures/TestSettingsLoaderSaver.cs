using UnityEngine;

public class TestSettingsLoaderSaver : ISettingsLoaderSaver
{
    public Settings LoadSettings()
    {
        Debug.Log("Returning new test settings");
        return new TestSettings();
    }

    public void SaveSettings(Settings settings)
    {
        Debug.Log("Not saving test settings.");
    }
}
