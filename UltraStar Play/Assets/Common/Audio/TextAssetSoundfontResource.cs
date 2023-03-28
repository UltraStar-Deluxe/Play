using System.IO;
using AudioSynthesis;
using UnityEngine;

public class TextAssetSoundfontResource : IResource
{
    private readonly TextAsset textAsset;
    
    public TextAssetSoundfontResource(TextAsset textAsset)
    {
        this.textAsset = textAsset;
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
        return textAsset.name;
    }

    public Stream OpenResourceForRead()
    {
        return new MemoryStream(textAsset.bytes);
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
