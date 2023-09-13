public class ModObjectContext
{
    public string ModFolder { get; private set; }
    public bool IsObsolete { get; private set; }

    public ModObjectContext(string modFolder, bool isObsolete)
    {
        ModFolder = modFolder;
        IsObsolete = isObsolete;
    }

    public void SetObsolete()
    {
        IsObsolete = true;
    }
}
