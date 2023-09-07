public class RuntimeLoadedScriptContext
{
    public string FolderPath { get; private set; }
    public bool IsInstanceObsolete { get; private set; }

    public RuntimeLoadedScriptContext(string folderPath, bool isInstanceObsolete)
    {
        FolderPath = folderPath;
        IsInstanceObsolete = isInstanceObsolete;
    }

    public void SetObsolete()
    {
        IsInstanceObsolete = true;
    }
}
