using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongDetailsPageObject : INeedInjection
{
    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.songDetailsContainer)]
    private VisualElement songDetailsContainer;

    [Inject]
    private SongListPageObject songListPageObject;

    [Inject(UxmlName = R.UxmlNames.enqueueButton)]
    private Button enqueueButton;

    public async Awaitable OpenAsync(string songTitle)
    {
        await songListPageObject.OpenAsync();
        songListPageObject.SetSearchText(songTitle);
        songListPageObject.GetFirstSongEntryButton().SendClickEvent();
        await ConditionUtils.WaitForConditionAsync(() => songDetailsContainer.IsVisibleByDisplay(),
            new WaitForConditionConfig { description = "shows song details" });
    }

    public void Enqueue()
    {
        enqueueButton.SendClickEvent();
    }
}
