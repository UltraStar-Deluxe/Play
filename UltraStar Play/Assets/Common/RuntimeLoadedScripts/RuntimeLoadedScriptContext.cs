public class RuntimeLoadedScriptContext
{
    public string ModFolder { get; private set; }
    public bool IsInstanceObsolete { get; private set; }

    public RuntimeLoadedScriptContext(string modFolder, bool isInstanceObsolete)
    {
        ModFolder = modFolder;
        IsInstanceObsolete = isInstanceObsolete;
    }

    public void SetObsolete()
    {
        IsInstanceObsolete = true;
    }
}
