using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongListPageObject : INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListView songListView;

    [Inject(UxmlName = R.UxmlNames.songSearchTextField)]
    private TextField songSearchTextField;

    public async Awaitable OpenSongListAsync()
    {
        showSongViewButton.SendClickEvent();
        await ConditionUtils.WaitForConditionAsync(() => songListView.itemsSource.Count > 1,
            new WaitForConditionConfig { description = "song list has multiple entries" });
    }

    public void SetSearchText(string searchText)
    {
        songSearchTextField.value = searchText;
    }

    public Button GetFirstSongEntryButton()
    {
        return songListView.Q<Button>(R.UxmlNames.songListEntryButton);
    }
}
