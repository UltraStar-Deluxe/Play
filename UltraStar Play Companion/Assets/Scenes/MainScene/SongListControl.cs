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

    private List<SongQueueEntryDto> SongQueueEntryDtos => songQueueUiControl.SongQueueEntryControls
        .Select(control => control.SongQueueEntryDto)
        .ToList();
    
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
        showSongQueueButton.RegisterCallbackButtonTriggered(_ => UpdateSongQueue());
        
        songListRequestor.SongListEventStream.Subscribe(evt => HandleSongListEvent(evt));

        songSearchTextField.RegisterValueChangedCallback(evt =>
        {
            songSearchHint.SetVisibleByDisplay(songSearchTextField.value.IsNullOrEmpty());
            UpdateSongSearchList();
        });

        mainGameHttpClient.Permissions.Subscribe(_ => UpdateSongQueue());
        
        songQueueUiControl.OnDelete = entry => DeleteSongQueueEntry(entry);
        songQueueUiControl.OnToggleMedley = entry => ToggleMedley(entry);
    }

    private void DeleteSongQueueEntry(SongQueueEntryDto entry)
    {
        mainGameHttpClient.DeleteRequest(HttpApiEndpointPaths.SongQueueEntryIndex
                .ReplaceOrThrow("{index}", SongQueueEntryDtos.IndexOf(entry).ToString()),
            response =>
            {
                UpdateSongQueue();
            },
            ex =>
            {
                UpdateSongQueue();
            });
    }

    private void ToggleMedley(SongQueueEntryDto entry)
    {
        entry.IsMedleyWithPreviousEntry = !entry.IsMedleyWithPreviousEntry;
        mainGameHttpClient.PostRequest(HttpApiEndpointPaths.SongQueueEntryIndex
                .ReplaceOrThrow("{index}", SongQueueEntryDtos.IndexOf(entry).ToString()),
            entry.ToJson(),
            MimeTypeUtils.ApplicationJson,
            response =>
            {
                UpdateSongQueue();
            },
            ex =>
            {
                UpdateSongQueue();
            });
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
            SongListEntryControl songListEntryControl = CreateSongListEntryControl(songDto);
            songsScrollView.Add(songListEntryControl.VisualElement);
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
            songsScrollView.Clear();
            songsScrollView.Add(new Label("Loading song list..."));
            songListRequestor.RequestSongList();
        }
    }

    private SongListEntryControl CreateSongListEntryControl(SongDto songDto)
    {
        VisualElement songListEntry = songListEntryUi.CloneTreeAndGetFirstChild();

        SongListEntryControl songListEntryControl = injector
            .WithRootVisualElement(songListEntry)
            .WithBindingForInstance(songDto)
            .WithBindingForInstance(songDetailsControl)
            .CreateAndInject<SongListEntryControl>();
        
        return songListEntryControl;
    }

    public void Dispose()
    {
        songDetailsControl.Dispose();
    }
}
