using System;
using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class TitleAndArtistText : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Text uiText;

    [Inject]
    private SongMeta songMeta;

    private string artist;
    private string title;

    void Start()
    {
        SetArtist(songMeta.Artist);
        SetTitle(songMeta.Title);
    }

    public void SetArtist(string artist)
    {
        this.artist = artist;
        UpdateUiText();
    }

    public void SetTitle(string title)
    {
        this.title = title;
        UpdateUiText();
    }

    private void UpdateUiText()
    {
        uiText.text = artist + "\n" + title;
    }
}
