using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Preloads song data (notably the song cover),
 * such that the data is already cached (in ImageManager and in RAM) and can be accessed with better performance when needed.
 */
public class SongSelectSongPreloader : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongRouletteControl songRouletteControl;

    /**
     * Number of songs before and after the currently visible songs that should be preloaded.
     */
    private int preloadSongCount = 5;

    private readonly Queue<SongMeta> preloadQueue = new Queue<SongMeta>();

    private float nextPreloadTime;

	private void Start()
    {
        if (preloadSongCount <= 0)
        {
            return;
        }
        songRouletteControl.Selection.Subscribe(songSelection => QueueSongsToPreload(songSelection.SongIndex));
    }

    private void Update()
    {
        if (!preloadQueue.IsNullOrEmpty()
            && nextPreloadTime <= Time.time)
        {
            SongMeta songMeta = preloadQueue.Dequeue();
            PreloadSong(songMeta);
        }
    }

    private void PreloadSong(SongMeta songMeta)
    {
        // Preload cover
        string coverPath = SongMetaUtils.GetAbsoluteSongCoverPath(songMeta);
        if (!coverPath.IsNullOrEmpty()
            && File.Exists(coverPath)
            && !ImageManager.IsSpriteCached(coverPath))
        {
            ImageManager.LoadSprite(coverPath);
            Debug.Log($"Preloaded cover of {songMeta.Title}: {coverPath}");
        }

        // Preload txt file
        songMeta.GetVoices();

        // Only preload few songs per second to distribute the load time.
        nextPreloadTime = Time.time + 0.25f;
    }

    private void QueueSongsToPreload(int currentSongIndex)
    {
        if (songRouletteControl.Songs.Count <= 5)
        {
            return;
        }

        void QueueSongToPreloadByIndex(int index)
        {
            int wrappedSongIndex = index % songRouletteControl.Songs.Count;
            SongMeta songMeta = songRouletteControl.GetSongAtIndex(wrappedSongIndex);
            if (songMeta != null)
            {
                preloadQueue.Enqueue(songMeta);
            }
        }

        int preloadCount = Mathf.Min(preloadSongCount, songRouletteControl.Songs.Count);

        int minVisibleIndex = currentSongIndex - songRouletteControl.SongEntryPlaceholderCount;
        int fromIndex = minVisibleIndex - preloadCount;
        for (int index = fromIndex; index < minVisibleIndex; index++)
        {
            QueueSongToPreloadByIndex(index);
        }

        int maxVisibleIndex = currentSongIndex + songRouletteControl.SongEntryPlaceholderCount;
        int toIndex = maxVisibleIndex + preloadCount;
        for (int index = maxVisibleIndex; index < toIndex; index++)
        {
            QueueSongToPreloadByIndex(index);
        }
    }
}
