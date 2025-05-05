public class ModObjectContext
{
    public string ModFolder { get; private set; }
    public string ModName { get; private set; }
    public string ModPersistentDataFolder { get; private set; }
    public bool IsObsolete { get; private set; }

    public ModObjectContext(
        string modFolder,
        string modName,
        string modPersistentDataFolder,
        bool isObsolete)
    {
        ModFolder = modFolder;
        ModName = modName;
        ModPersistentDataFolder = modPersistentDataFolder;
        IsObsolete = isObsolete;
    }

    public void SetObsolete()
    {
        IsObsolete = true;
    }
}
