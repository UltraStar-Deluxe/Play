public interface ISettingsLoaderSaver
{
    Settings LoadSettings();
    void SaveSettings(Settings settings);
}
