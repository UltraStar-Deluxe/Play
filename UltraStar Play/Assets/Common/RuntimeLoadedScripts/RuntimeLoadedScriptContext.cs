public class RuntimeLoadedScriptContext
{
    public string FolderPath { get; private set; }

    public RuntimeLoadedScriptContext(string folderPath)
    {
        FolderPath = folderPath;
    }
}
