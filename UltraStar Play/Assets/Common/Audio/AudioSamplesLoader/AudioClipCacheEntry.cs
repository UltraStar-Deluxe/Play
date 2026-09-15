using System;
using UnityEngine;

public class AudioClipCacheEntry
{
    public AudioClip AudioClip { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastUsedAt { get; private set; }

    public AudioClipCacheEntry(AudioClip audioClip)
    {
        AudioClip = audioClip;
        CreatedAt = DateTime.Now;
        LastUsedAt = DateTime.Now;
    }

    public void UpdateLastUsedAt()
    {
        LastUsedAt = DateTime.Now;
    }
}
