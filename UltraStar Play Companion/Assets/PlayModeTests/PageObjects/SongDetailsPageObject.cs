using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongDetailsPageObject : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.songDetailsContainer)]
    private VisualElement songDetailsContainer;

    private SongListPageObject songListPageObject;

    public void OnInjectionFinished()
    {
        songListPageObject = injector.CreateAndInject<SongListPageObject>();
    }

    public async Awaitable OpenSongDetailsAsync(string songTitle)
    {
        await songListPageObject.OpenSongListAsync();

        songListPageObject.SetSearchText(songTitle);
        songListPageObject.GetFirstSongEntryButton().SendClickEvent();
        await ConditionUtils.WaitForConditionAsync(() => songDetailsContainer.IsVisibleByDisplay(),
            new WaitForConditionConfig { description = "shows song details" });
    }
}
