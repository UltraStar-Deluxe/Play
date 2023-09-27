public class ModObjectContext
{
    public string ModFolder { get; private set; }
    public string ModSettingsFolder { get; private set; }
    public bool IsObsolete { get; private set; }

    public ModObjectContext(
        string modFolder,
        string modSettingsFolder,
        bool isObsolete)
    {
        ModFolder = modFolder;
        ModSettingsFolder = modSettingsFolder;
        IsObsolete = isObsolete;
    }

    public void SetObsolete()
    {
        IsObsolete = true;
    }
}
