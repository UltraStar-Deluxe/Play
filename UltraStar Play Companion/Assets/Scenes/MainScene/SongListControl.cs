using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongListControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    private const float SongQueueItemHeightInPx = 80;

    private IComparer<SongDto> songDtoComparer = new SongListSongDtoComparer();

    [Inject(Key = nameof(songListEntryUi))]
    private VisualTreeAsset songListEntryUi;

    [Inject(UxmlName = R.UxmlNames.songSearchTextField)]
    private TextField songSearchTextField;

    [Inject(UxmlName = R.UxmlNames.songSearchHint)]
    private Label songSearchHint;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListView songListView;

    [Inject(UxmlName = R.UxmlNames.songListContainer)]
    private VisualElement songListContainer;

    [Inject(UxmlName = R.UxmlNames.songViewContainer)]
    private VisualElement songViewContainer;

    [Inject(UxmlName = R.UxmlNames.showSongSearchButton)]
    private Button showSongSearchButton;

    [Inject(UxmlName = R.UxmlNames.showSongQueueButton)]
    private Button showSongQueueButton;

    [Inject(UxmlName = R.UxmlNames.songSearchContainer)]
    private VisualElement songSearchContainer;

    [Inject(UxmlName = R.UxmlNames.songQueueContainer)]
    private VisualElement songQueueContainer;

    [Inject(UxmlName = R.UxmlNames.songDetailsContainer)]
    private VisualElement songDetailsContainer;

    [Inject(UxmlName = R.UxmlNames.songListStatusLabel)]
    private Label songListStatusLabel;

    [Inject]
    private SongListRequestor songListRequestor;

    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntriesListView)]
    private ListView listView;

    private readonly TabGroupControl tabGroupControl = new();
    private readonly SongDetailsControl songDetailsControl = new();
    private readonly SongQueueUiControl songQueueUiControl = new();

    private ScrollView songListViewScrollView;
    private Vector2 songListViewScrollPosBeforeHide = new Vector2(-1, -1);
    private bool lastIsSongListVisibleByDisplay;

    private List<VisualElement> songListViewAncestors;

    public void OnInjectionFinished()
    {
        injector
            .WithRootVisualElement(songDetailsContainer)
            .Inject(songDetailsControl);
        injector
            .WithRootVisualElement(songQueueContainer)
            .Inject(songQueueUiControl);

        songListViewScrollView = songListView.Q<ScrollView>();
        songListViewAncestors = songListView.GetAncestors();

        tabGroupControl.AddTabGroupButton(showSongSearchButton, songSearchContainer);
        tabGroupControl.AddTabGroupButton(showSongQueueButton, songQueueContainer);
        tabGroupControl.ShowContainer(songSearchContainer);
        showSongQueueButton.RegisterCallbackButtonTriggered(_ => UpdateSongQueue());

        songListRequestor.SongListEventStream.Subscribe(evt => HandleSongListEvent(evt));

        songSearchTextField.RegisterValueChangedCallback(evt =>
        {
            songSearchHint.SetVisibleByDisplay(songSearchTextField.value.IsNullOrEmpty());
            UpdateSongListViewItems();
        });

        mainGameHttpClient.Permissions.Subscribe(_ => UpdateSongQueue());

        songQueueUiControl.OnDelete = DeleteSongQueueEntry;
        songQueueUiControl.OnToggleMedley = ToggleMedley;
        songQueueUiControl.OnItemIndexChanged = OnSongQueueItemIndexChanged;
        songQueueUiControl.ItemHeight = SongQueueItemHeightInPx;

        songListView.makeItem = OnMakeItem;
        songListView.bindItem = OnBindItem;
        songListView.unbindItem = OnUnbindItem;
    }

    public void LateUpdate()
    {
        bool isSongListVisibleByDisplay = songListView.IsVisibleByDisplay()
            && songListViewAncestors.AllMatch(ancestor => ancestor.IsVisibleByDisplay());

        // Restore last scroll position when list gets visible.
        // Sadly required because Unity does not handle this: https://forum.unity.com/threads/scrollview-loses-scroll-position-after-hide-and-show-display-none-display-flex.1084706/
        // LateUpdate is used, because the ScrollView does not populate its content immediately when the style changes.
        if (isSongListVisibleByDisplay
            && !lastIsSongListVisibleByDisplay
            && songListViewScrollPosBeforeHide.x >= 0
            && songListViewScrollPosBeforeHide.y >= 0)
        {
            songListViewScrollView.scrollOffset = songListViewScrollPosBeforeHide;
            songListViewScrollPosBeforeHide = new Vector2(-1, -1);
        }
        else if (isSongListVisibleByDisplay)
        {
            songListViewScrollPosBeforeHide = songListViewScrollView.scrollOffset;
        }

        lastIsSongListVisibleByDisplay = isSongListVisibleByDisplay;
    }

    private void OnBindItem(VisualElement visualElement, int index)
    {
        List<SongDto> songDtos = songListView.itemsSource as List<SongDto>;
        if (visualElement.userData is SongListEntryControl songListEntryControl
            && index >= 0 && index < songDtos.Count)
        {
            songListEntryControl.SongDto = songDtos[index];
        }
    }

    private void OnUnbindItem(VisualElement visualElement, int index)
    {
        if (visualElement.userData is SongListEntryControl songListEntryControl)
        {
            songListEntryControl.SongDto = null;
        }
    }

    private VisualElement OnMakeItem()
    {
        VisualElement songListEntry = songListEntryUi.CloneTreeAndGetFirstChild();

        injector
            .WithRootVisualElement(songListEntry)
            .WithBindingForInstance(songDetailsControl)
            .CreateAndInject<SongListEntryControl>();

        return songListEntry;
    }

    private async void DeleteSongQueueEntry(SongQueueEntryDto entry)
    {
        try
        {
            await mainGameHttpClient.DeleteRequestAsync(RestApiEndpointPaths.SongQueueEntryIndex
                .ReplaceOrThrow("{index}", songQueueUiControl.GetSongQueueEntryIndex(entry).ToString()));
        }
        finally
        {
            UpdateSongQueue();
        }
    }

    private async void ToggleMedley(SongQueueEntryDto entry)
    {
        entry.IsMedleyWithPreviousEntry = !entry.IsMedleyWithPreviousEntry;
        try
        {
            await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.SongQueueEntryIndex
                    .ReplaceOrThrow("{index}", songQueueUiControl.GetSongQueueEntryIndex(entry).ToString()),
                entry.ToJson());
        }
        finally
        {
            UpdateSongQueue();
        }
    }

    private async void OnSongQueueItemIndexChanged(SongQueueUiControl.ItemIndexChangedEvent evt)
    {
        Debug.Log($"OnSongQueueItemIndexChanged: {evt.OldIndex}, {evt.NewIndex}");
        string json = new ListDto<SongQueueEntryDto>(evt.UpdatedItems.ToList()).ToJson();
        await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.SongQueue, json);
    }

    private async void UpdateSongQueue()
    {
        songQueueUiControl.Clear();

        if (!mainGameHttpClient.IsConnected)
        {
            return;
        }

        try
        {
            string response = await mainGameHttpClient.GetRequestAsync(RestApiEndpointPaths.SongQueue);
            ListDto<SongQueueEntryDto> listDto = JsonConverter.FromJson<ListDto<SongQueueEntryDto>>(response);
            if (listDto == null)
            {
                songQueueContainer.Add(new Label("Failed to load song queue."));
                return;
            }

            songQueueUiControl.HasWriteSongQueuePermission = mainGameHttpClient.Permissions.Value.Contains(RestApiPermission.WriteSongQueue);
            songQueueUiControl.SetSongQueueEntryDtos(listDto.Items);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            songQueueContainer.Add(new Label("Failed to load song queue."));
        }
    }

    private void UpdateSongListViewItems()
    {
        if (songListRequestor.LoadedSongsDto == null
            || songListRequestor.LoadedSongsDto.SongList.IsNullOrEmpty())
        {
            SetSongListStatus("No songs found");
            return;
        }

        List<SongDto> songDtos = new List<SongDto>(songListRequestor.LoadedSongsDto.SongList)
            .Where(songDto => SongSearchMatches(songDto))
            .ToList();
        songDtos.Sort(songDtoComparer);

        songListView.itemsSource = songDtos;
        songListView.RefreshItems();

        if (songListRequestor.LoadedSongsDto.IsSongScanFinished)
        {
            SetSongListStatus("");
        }
        else
        {
            SetSongListStatus("Loading song list...");
        }
    }

    private bool SongSearchMatches(SongDto songDto)
    {
        string searchText = songSearchTextField.value.ToLowerInvariant();
        return searchText.IsNullOrEmpty()
               || StringUtils.ContainsIgnoreCaseAndDiacritics(songDto.Title, searchText)
               || StringUtils.ContainsIgnoreCaseAndDiacritics(songDto.Artist, searchText);
    }

    private void HandleSongListEvent(SongListEvent evt)
    {
        if (!evt.ErrorMessage.IsNullOrEmpty())
        {
            ClearSongList();
            SetSongListStatus(evt.ErrorMessage);
            return;
        }

        UpdateSongListViewItems();
    }

    private void ClearSongList()
    {
        songListView.itemsSource = new List<SongDto>();
    }

    public void UpdateTranslation()
    {
        // Search text field hint
        string searchPropertiesCsv = new List<Translation>
        {
            Translation.Get(R.Messages.enum_SongProperty_Artist),
            Translation.Get(R.Messages.enum_SongProperty_Title),
        }.JoinWith(", ");
        songSearchHint.SetTranslatedText(Translation.Of($"Search in {searchPropertiesCsv}"));
    }

    public void Show()
    {
        if (songQueueContainer.IsVisibleByDisplay())
        {
            UpdateSongQueue();
        }
        else
        {
            ShowSongList();
        }
    }

    private void ShowSongList()
    {
        songSearchTextField.value = "";

        if (!songListRequestor.SuccessfullyLoadedAllSongs)
        {
            ClearSongList();
            SetSongListStatus("Loading song list...");
            songListRequestor.RequestSongList();
        }
    }

    private void SetSongListStatus(string text)
    {
        songListStatusLabel.SetVisibleByDisplay(!text.IsNullOrEmpty());
        songListStatusLabel.text = text;
    }

    public void Dispose()
    {
        songDetailsControl.Dispose();
    }
}
