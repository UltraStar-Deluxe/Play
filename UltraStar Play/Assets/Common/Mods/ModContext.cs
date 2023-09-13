public class ModContext
{
    public string ModFolder { get; private set; }

    public object UserData { get; set; }

    public ModContext(string modFolder)
    {
        ModFolder = modFolder;
    }
}
