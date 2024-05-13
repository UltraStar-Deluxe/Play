using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class SongMetaCollection
{
    private ConcurrentBag<SongMeta> songMetas = new();
    public IReadOnlyCollection<SongMeta> SongMetas => songMetas;

    private readonly Subject<SongMeta> addedSongMetaEventStream = new();
    public IObservable<SongMeta> AddedSongMetaEventStream => addedSongMetaEventStream;

    public int Count => songMetas.Count;

    public void Clear()
    {
        songMetas = new();
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
