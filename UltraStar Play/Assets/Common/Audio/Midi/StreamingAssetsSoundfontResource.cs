using System.IO;
using AudioSynthesis;

public class StreamingAssetsSoundfontResource : IResource
{
    private readonly string streamingAssetsRelativePath;

    public StreamingAssetsSoundfontResource()
    {
    }

    public StreamingAssetsSoundfontResource(string streamingAssetsRelativePath)
    {
        this.streamingAssetsRelativePath = streamingAssetsRelativePath;
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
        return Path.GetFileName(streamingAssetsRelativePath);
    }

    public Stream OpenResourceForRead()
    {
        string absolutePath = ApplicationUtils.GetStreamingAssetsPath(streamingAssetsRelativePath);
        return File.OpenRead(absolutePath);
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
