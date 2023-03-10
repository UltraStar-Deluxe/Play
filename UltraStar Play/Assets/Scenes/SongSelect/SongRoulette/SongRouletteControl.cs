using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteControl : MonoBehaviour, INeedInjection
{
    public VisualTreeAsset songEntryUi;
    
    [Inject]
    private Injector injector;

    [Inject]
    private SongSelectSongPreviewControl songPreviewControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListView songListView;
    
    [Inject(UxmlName = R.UxmlNames.mediumDifficultyButton)]
    private Button mediumDifficultyButton;
    
    private List<SongMeta> songs = new();
    public IReadOnlyList<SongMeta> Songs => songs;

    public IReactiveProperty<SongSelection> Selection { get; private set; } = new ReactiveProperty<SongSelection>();

    private readonly Subject<SongSelection> selectionClickedEventStream = new();
    public IObservable<SongSelection> SelectionClickedEventStream => selectionClickedEventStream;

    private readonly Subject<List<SongMeta>> songListChangedEventStream = new();
    public IObservable<List<SongMeta>> SongListChangedEventStream => songListChangedEventStream;

    private int SelectedSongIndex
    {
        get
        {
            return (Selection.Value.SongMeta == null) ? -1 : Selection.Value.SongIndex;
        }
    }

    private SongMeta SelectedSongMeta
    {
        get
        {
            return Selection.Value.SongMeta;
        }
    }
    
    private readonly List<SongEntryControl> songEntryControls = new();
    public IReadOnlyList<SongEntryControl> SongEntryControls => songEntryControls;
    public SongEntryControl SelectedSongEntryControl => songEntryControls
        .FirstOrDefault(it => it.SongMeta == Selection.Value.SongMeta);

    private void Start()
    {
        songListView.makeItem = () =>
        {
            VisualElement songEntryVisualElement = songEntryUi.CloneTree().Children().FirstOrDefault();
            return songEntryVisualElement;
        };
        songListView.bindItem = (VisualElement element, int index) =>
        {
            SongMeta songMeta = songs[index];
            element.userData = songMeta;
            CreateSongEntryControl(songMeta, element);
        };
        songListView.selectionChanged += OnSongListViewSelectionChanged;
    }

    private void OnSongListViewSelectionChanged(IEnumerable<object> selectedObjects)
    {
        SongMeta selectedSongMeta = selectedObjects.FirstOrDefault() as SongMeta;
        SelectSong(selectedSongMeta);
    }

    private void CreateSongEntryControl(SongMeta songMeta, VisualElement songEntryVisualElement)
    {
        SongEntryControl item = injector
            .WithRootVisualElement(songEntryVisualElement)
            .CreateAndInject<SongEntryControl>();
        item.Name = songMeta.Artist + "-" + songMeta.Title;
        item.SongMeta = songMeta;

        item.ClickEventStream.Subscribe(_ => OnSongButtonClicked(songMeta));

        songEntryControls.Add(item);
    }

    public void SetSongs(IReadOnlyCollection<SongMeta> songMetas)
    {
        int lastSelectedSongIndex = NumberUtils.Limit(SelectedSongIndex, 0, songMetas.Count - 1);
        SongMeta lastSelectedSongMeta = Selection.Value.SongMeta;
        songs = new List<SongMeta>(songMetas);
        if (songs.Count > 0)
        {
            // Try to restore song selection
            if (lastSelectedSongMeta != null)
            {
                int songIndex = GetSongIndex(lastSelectedSongMeta);
                if (songIndex >= 0)
                {
                    lastSelectedSongIndex = songIndex;
                }
            }

            Selection.Value = new SongSelection(songs[lastSelectedSongIndex], lastSelectedSongIndex, songs.Count);
        }
        else
        {
            Selection.Value = new SongSelection(null, -1, 0);
        }

        if (!VisualElementUtils.HasGeometry(songListView))
        {
            songListView.RegisterCallbackOneShot<GeometryChangedEvent>(_ => UpdateListViewItems());
        }
        else
        {
            UpdateListViewItems();
        }
        
        songListChangedEventStream.OnNext(songs);
    }

    private void UpdateListViewItems()
    {
        songListView.itemsSource = new List<SongMeta>(songs);
        songListView.RefreshItems();
        if (Selection.Value.SongMeta != null)
        {
            songListView.SetSelectionAndScrollTo(Selection.Value.SongIndex);
        }
    }

    private int GetSongIndex(SongMeta songMeta)
    {
        return songs.IndexOf(songMeta);
    }

    public void SelectSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        int songIndex = songs.IndexOf(songMeta);
        if (songListView.selectedIndex != songIndex)
        {
            songListView.SetSelectionAndScrollTo(songIndex);
        }
        Selection.Value = new SongSelection(songMeta, songIndex, songs.Count);
    }

    public void SelectSongByIndex(int index, bool wrapAround = true)
    {
        if (!wrapAround
            && (index < 0 || songs.Count <= index))
        {
            // Ignore out-of-range index
            return;
        }

        SongMeta nextSong = GetSongAtIndex(index);
        SelectSong(nextSong);
    }

    public SongMeta Find(Predicate<SongMeta> predicate)
    {
        return songs.Find(predicate);
    }

    public void SelectNextSong()
    {
        int nextIndex;
        if (SelectedSongIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = SelectedSongIndex + 1;
        }
        SelectSongByIndex(nextIndex);
    }

    public void SelectPreviousSong()
    {
        int nextIndex;
        if (SelectedSongIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = SelectedSongIndex - 1;
        }
        SelectSongByIndex(nextIndex);
    }

    public void SelectRandomSong()
    {
        int songIndex = new System.Random().Next(0, songs.Count - 1);
        SelectSongByIndex(songIndex);
    }

    public SongMeta GetSongAtIndex(int index)
    {
        if (songs.Count == 0)
        {
            return null;
        }
        int wrappedIndex = (index < 0) ? index + songs.Count : index;
        int wrappedIndexModulo = wrappedIndex % songs.Count;
        if (wrappedIndexModulo < 0)
        {
            wrappedIndexModulo = 0;
        }
        SongMeta song = songs[wrappedIndexModulo];
        return song;
    }

    private void OnSongButtonClicked(SongMeta songMeta)
    {
        if (Selection.Value.SongMeta != null
            && Selection.Value.SongMeta == songMeta)
        {
            selectionClickedEventStream.OnNext(Selection.Value);
        }
        else
        {
            SelectSong(songMeta);
        }
    }

    private T FindNearestSlot<T>(float targetPositionX, List<T> allEntries)
        where T : SongEntryPlaceholderControl
    {
        return allEntries.FindMinElement(entry =>
        {
            float x = entry.VisualElement.worldBound.center.x;
            float distance = Mathf.Abs(x - targetPositionX);
            return distance;
        });
    }
}
