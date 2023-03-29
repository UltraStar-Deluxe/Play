using System.IO;
using AudioSynthesis;

public class FileSystemSoundfontResource : IResource
{
    private readonly string path;
    
    public FileSystemSoundfontResource(string path)
    {
        this.path = path;
    }

    public bool ReadAllowed()
    {
        return true;
    }

    public bool WriteAllowed()
    {
        return false;
    }

    public bool DeleteAllowed()
    {
        return false;
    }

    public string GetName()
    {
        return Path.GetFileName(path);
    }

    public Stream OpenResourceForRead()
    {
        return File.OpenRead(path);
    }

    public Stream OpenResourceForWrite()
    {
        throw new System.NotImplementedException();
    }

    public void DeleteResource()
    {
        throw new System.NotImplementedException();
    }
}
