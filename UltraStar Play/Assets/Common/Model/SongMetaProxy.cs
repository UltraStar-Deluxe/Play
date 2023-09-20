using System;
using System.Collections.Generic;
using UnityEngine;

public class SongMetaProxy : SongMeta
{
    private readonly Dictionary<string, object> preloadedHeaderFields = new();
    private Dictionary<string, object> PreloadedHeaderFields => preloadedHeaderFields;

    private SongMeta songMeta;
    public SongMeta SongMeta
    {
        get
        {
            if (!isResolved)
            {
                isResolved = true;
                try
                {
                    songMeta = songMetaGetter();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to get song meta: {ex.Message}");
                }
            }

            return songMeta;
        }
    }
    private readonly Func<SongMeta> songMetaGetter;
    private bool isResolved;

    public SongMetaProxy(Func<SongMeta> songMetaGetter)
    {
        this.songMetaGetter = songMetaGetter;
    }

    public void SetHeaderField(string key, object value)
    {
        preloadedHeaderFields[key] = value;
    }

    public void RemoveHeaderField(string key)
    {
        preloadedHeaderFields.Remove(key);
    }

    protected override List<Voice> DoLoadVoices()
    {
        return new List<Voice>();
    }
}
