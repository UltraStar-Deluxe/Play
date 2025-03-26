using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public class SongMetaCollection
{
    private ConcurrentBag<SongMeta> songMetas = new();
    public IReadOnlyCollection<SongMeta> SongMetas => songMetas;

    private readonly Subject<SongMeta> addedSongMetaEventStream = new();
    public IObservable<SongMeta> AddedSongMetaEventStream => addedSongMetaEventStream;

    private readonly Subject<SongMeta> removedSongMetaEventStream = new();
    public IObservable<SongMeta> RemovedSongMetaEventStream => removedSongMetaEventStream;

    public int Count => songMetas.Count;

    public void Clear()
    {
        songMetas = new();
    }

    /**
     * Removes an entry.
     * Therefore, copies the current entries, except for the one to be removed.
     * Note that this is unsafe in a multithreaded context because entries that are added
     * after the copy is created and before the new collection is set will be lost.
     */
    public void RemoveUnsafe(SongMeta songMeta)
    {
        List<SongMeta> songMetasCopy = songMetas.ToList();
        songMetasCopy.Remove(songMeta);
        songMetas = new ConcurrentBag<SongMeta>(songMetasCopy);
        removedSongMetaEventStream.OnNext(songMeta);
    }

    public void Add(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        songMetas.Add(songMeta);
        try
        {
            addedSongMetaEventStream.OnNext(songMeta);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to notify about added song: '{songMeta.GetArtistDashTitle()}': {ex.Message}");
        }
    }

}
