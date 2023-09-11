public class ModContext
{
    public string ModFolder { get; private set; }

    public ModContext(string modFolder)
    {
        ModFolder = modFolder;
    }
}
