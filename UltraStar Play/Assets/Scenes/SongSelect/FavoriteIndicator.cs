using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UniRx.Triggers;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class FavoriteIndicator : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Sprite favoriteSprite;
    [InjectedInInspector]
    public Sprite noFavoriteSprite;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Image image;

    [Inject]
    private SongRouletteController songRouletteController;

    [Inject]
    private SongSelectSceneController songSelectSceneController;

    [Inject]
    private PlaylistManager playlistManager;

    void Start()
    {
        songRouletteController.Selection.Subscribe(newSelection => UpdateImage(newSelection.SongMeta));
        image.OnPointerClickAsObservable().Subscribe(_ => songSelectSceneController.ToggleSelectedSongIsFavorite());

        playlistManager.PlaylistChangeEventStream.Subscribe(playlistChangeEvent =>
        {
            if (songRouletteController.Selection.Value.SongMeta == playlistChangeEvent.SongMeta)
            {
                UpdateImage(playlistChangeEvent.SongMeta);
            }
        });
    }

    public void UpdateImage(SongMeta songMeta)
    {
        Sprite sprite = IsFavorite(songMeta)
            ? favoriteSprite
            : noFavoriteSprite;

        image.sprite = sprite;
        image.enabled = songMeta != null;
    }

    private bool IsFavorite(SongMeta songMeta)
    {
        return songMeta != null
            && playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title);
    }
}
