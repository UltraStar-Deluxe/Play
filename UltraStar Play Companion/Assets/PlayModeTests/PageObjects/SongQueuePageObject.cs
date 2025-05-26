using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongQueuePageObject : INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.showSongQueueButton)]
    private Button showSongQueueButton;

    [Inject(UxmlName = R.UxmlNames.songQueueContainer)]
    private VisualElement songQueueContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntriesListView)]
    private ListView songQueueEntriesListView;

    public async Awaitable OpenAsync()
    {
        showSongViewButton.SendClickEvent();
        showSongQueueButton.SendClickEvent();

        await Awaitable.WaitForSecondsAsync(1);
    }

    public async Task RemoveAllAsync()
    {
        List<VisualElement> songQueueEntries = songQueueEntriesListView.Query(R_PlayShared.UxmlNames.songQueueEntryUiRoot).ToList();
        foreach (VisualElement songQueueEntry in songQueueEntries)
        {
            Button deleteButton = songQueueEntry.Q<Button>(R_PlayShared.UxmlNames.deleteButton);
            deleteButton.SendClickEvent();
        }

        await ConditionUtils.WaitForConditionAsync(() => songQueueEntriesListView.itemsSource.Count == 0,
            new WaitForConditionConfig { description = "song queue is empty" });
    }

    public IList GetEntries()
    {
        return songQueueEntriesListView.itemsSource;
    }
}
