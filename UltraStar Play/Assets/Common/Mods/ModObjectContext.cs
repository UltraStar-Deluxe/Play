public class ModObjectContext
{
    public string ModFolder { get; private set; }
    public string ModPersistentDataFolder { get; private set; }
    public bool IsObsolete { get; private set; }

    public ModObjectContext(
        string modFolder,
        string modPersistentDataFolder,
        bool isObsolete)
    {
        ModFolder = modFolder;
        ModPersistentDataFolder = modPersistentDataFolder;
        IsObsolete = isObsolete;
    }

    public void SetObsolete()
    {
        IsObsolete = true;
    }
}
