using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongListPageObject : INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.showSongSearchButton)]
    private Button showSongSearchButton;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListView songListView;

    [Inject(UxmlName = R.UxmlNames.songSearchTextField)]
    private TextField songSearchTextField;

    public async Awaitable OpenAsync()
    {
        showSongViewButton.SendClickEvent();
        showSongSearchButton.SendClickEvent();

        await Awaitable.WaitForSecondsAsync(1);
    }

    public void SetSearchText(string searchText)
    {
        songSearchTextField.value = searchText;
    }

    public Button GetFirstSongEntryButton()
    {
        return songListView.Q<Button>(R.UxmlNames.songListEntryButton);
    }

    public IList GetEntries()
    {
        return songListView.itemsSource;
    }
}
