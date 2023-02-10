using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongListControl : INeedInjection, IInjectionFinishedListener, ITranslator, IDisposable
{
    [Inject(Key = nameof(songListEntryUi))]
    private VisualTreeAsset songListEntryUi;
    
    [Inject(UxmlName = R.UxmlNames.songSearchTextField)]
    private TextField songSearchTextField;

    [Inject(UxmlName = R.UxmlNames.songSearchHint)]
    private Label songSearchHint;

    [Inject(UxmlName = R.UxmlNames.songsScrollView)]
    private ScrollView songsScrollView;
    
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
    
    [Inject]
    private SongListRequestor songListRequestor;

    [Inject]
    private MainGameHttpClient mainGameHttpClient;
    
    [Inject]
    private Injector injector;

    private readonly TabGroupControl tabGroupControl = new();
    private readonly SongDetailsControl songDetailsControl = new();
    private readonly SongQueueUiControl songQueueUiControl = new();

    public void OnInjectionFinished()
    {
        injector
            .WithRootVisualElement(songDetailsContainer)
            .Inject(songDetailsControl);
        injector
            .WithRootVisualElement(songQueueContainer)
            .Inject(songQueueUiControl);

        tabGroupControl.AddTabGroupButton(showSongSearchButton, songSearchContainer);
        tabGroupControl.AddTabGroupButton(showSongQueueButton, songQueueContainer);
        tabGroupControl.ShowContainer(songSearchContainer);
        showSongQueueButton.RegisterCallbackButtonTriggered(() => UpdateSongQueue());
        
        songListRequestor.SongListEventStream.Subscribe(evt => HandleSongListEvent(evt));

        songSearchTextField.RegisterValueChangedCallback(evt =>
        {
            songSearchHint.SetVisibleByDisplay(songSearchTextField.value.IsNullOrEmpty());
            UpdateSongSearchList();
        });

        mainGameHttpClient.Permissions.Subscribe(_ => UpdateSongQueue());
    }

    private void UpdateSongQueue()
    {
        songQueueUiControl.Clear();
        
        if (!mainGameHttpClient.IsConnected)
        {
            return;
        }

        mainGameHttpClient.GetRequest(HttpApiEndpointPaths.SongQueue,
            response =>
            {
                ListDto<SongQueueEntryDto> listDto = JsonConverter.FromJson<ListDto<SongQueueEntryDto>>(response, false);
                if (listDto == null)
                {
                    songQueueContainer.Add(new Label("Failed to load song queue."));
                    return;
                }

                songQueueUiControl.SetSongQueueEntryDtos(listDto.Items);
                
                if (!mainGameHttpClient.Permissions.Value.Contains(HttpApiPermission.WriteSongQueue))
                {
                    songQueueUiControl.HideControls();
                }
            }, 
            ex =>
            {
                songQueueContainer.Add(new Label("Failed to load song queue."));
                Debug.LogException(ex);
            });
    }

    private void UpdateSongSearchList()
    {
        songsScrollView.Clear();

        if (songListRequestor.LoadedSongsDto == null
            || songListRequestor.LoadedSongsDto.SongList.IsNullOrEmpty())
        {
            songsScrollView.Add(new Label("No songs found"));
            return;
        }

        List<SongDto> songDtos = new List<SongDto>(songListRequestor.LoadedSongsDto.SongList)
            .Where(songDto => SongSearchMatches(songDto))
            .ToList();
        songDtos.Sort((a,b) => string.Compare(a.Artist, b.Artist, StringComparison.InvariantCulture));

        foreach (SongDto songDto in songDtos)
        {
            VisualElement songListEntry = CreateSongListEntry(songDto);
            songsScrollView.Add(songListEntry);
        }

        if (!songListRequestor.LoadedSongsDto.IsSongScanFinished)
        {
            songsScrollView.Add(new Label("..."));
        }
    }
    
    private bool SongSearchMatches(SongDto songDto)
    {
        string searchText = songSearchTextField.value.ToLowerInvariant();
        return searchText.IsNullOrEmpty()
               || songDto.Title.ToLowerInvariant().Contains(searchText)
               || songDto.Artist.ToLowerInvariant().Contains(searchText);
    }

    private void HandleSongListEvent(SongListEvent evt)
    {
        if (!evt.ErrorMessage.IsNullOrEmpty())
        {
            songsScrollView.Clear();
            songsScrollView.Add(new Label(evt.ErrorMessage));
            return;
        }

        UpdateSongSearchList();
    }

    public void UpdateTranslation()
    {
        // Search text field hint
        string searchPropertiesText = new List<string>
        {
            TranslationManager.GetTranslation(R.Messages.songProperty_artist),
            TranslationManager.GetTranslation(R.Messages.songProperty_title),
        }.ToCsv(", ", "", "");
        songSearchHint.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_searchTextFieldHint, "properties", searchPropertiesText);
    }

    public void ShowSongList()
    {
        songSearchTextField.value = "";
                
        if (!songListRequestor.SuccessfullyLoadedAllSongs)
        {
            songsScrollView.Clear();
            songsScrollView.Add(new Label("Loading song list..."));
            songListRequestor.RequestSongList();
        }
    }

    private VisualElement CreateSongListEntry(SongDto songDto)
    {
        VisualElement songListEntry = songListEntryUi.CloneTreeAndGetFirstChild();
        songListEntry.Q<Label>(R.UxmlNames.songListEntryLabel).text = $"{songDto.Artist} - {songDto.Title}";
        songListEntry.Q<Button>(R.UxmlNames.songListEntryButton).RegisterCallbackButtonTriggered(() => songDetailsControl.ShowSongDetails(songDto));
        return songListEntry;
    }

    public void Dispose()
    {
        songDetailsControl.Dispose();
    }
}
