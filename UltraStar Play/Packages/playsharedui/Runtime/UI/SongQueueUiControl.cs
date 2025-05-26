using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(songQueueEntryUi))]
    private VisualTreeAsset songQueueEntryUi;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntriesListView)]
    private ListView listView;

    [Inject]
    private Injector injector;

    private IReadOnlyList<SongQueueEntryDto> songQueueEntryDtos = new List<SongQueueEntryDto>();

    public Action<SongQueueEntryDto> OnDelete { get; set; }
    public Action<SongQueueEntryDto> OnToggleMedley { get; set; }
    public Action<ItemIndexChangedEvent> OnItemIndexChanged { get; set; }
    public bool HasWriteSongQueuePermission { get; set; } = true;

    public float ItemHeight
    {
        get => listView.fixedItemHeight;
        set => listView.fixedItemHeight = value;
    }

    public void OnInjectionFinished()
    {
        listView.makeItem = OnMakeItem;
        listView.bindItem = OnBindItem;
        listView.unbindItem = OnUnbindItem;
        listView.itemIndexChanged += OnItemIndexChangedInternal;

        Clear();
    }

    private void OnItemIndexChangedInternal(int oldIndex, int newIndex)
    {
        songQueueEntryDtos = listView.itemsSource as List<SongQueueEntryDto>;

        // First entry cannot be medley with previous entry.
        if (!songQueueEntryDtos.IsNullOrEmpty())
        {
            songQueueEntryDtos[0].IsMedleyWithPreviousEntry = false;
        }

        OnItemIndexChanged?.Invoke(new ItemIndexChangedEvent(oldIndex, newIndex, songQueueEntryDtos));
    }

    private void OnBindItem(VisualElement element, int index)
    {
        SongQueueEntryUiControl entryControl = element.userData as SongQueueEntryUiControl;
        SongQueueEntryDto songQueueEntryDto = songQueueEntryDtos[index];

        entryControl.SongQueueEntryDto = songQueueEntryDto;
        entryControl.OnDelete = () => OnDelete?.Invoke(songQueueEntryDto);
        entryControl.OnToggleMedley = () => OnToggleMedley?.Invoke(songQueueEntryDto);

        // Hide medley button of first entry.
        if (index == 0)
        {
            entryControl.HideToggleMedleyButton();
        }
        else
        {
            entryControl.ShowToggleMedleyButton();
        }

        // Remove borders of medley entries.
        SongQueueEntryDto nextSongQueueEntryDto = CollectionUtils.SafeGet(songQueueEntryDtos, index + 1, null);
        element.EnableInClassList("medleyWithPrevious", songQueueEntryDto.IsMedleyWithPreviousEntry);
        element.EnableInClassList("medleyWithNext", nextSongQueueEntryDto?.IsMedleyWithPreviousEntry ?? false);

        // Hide controls if missing permissions
        if (!HasWriteSongQueuePermission)
        {
            entryControl.HideControls();
            listView.reorderable = false;
        }
        listView.reorderable = HasWriteSongQueuePermission;
    }

    private void OnUnbindItem(VisualElement element, int index)
    {
        SongQueueEntryUiControl entryControl = element.userData as SongQueueEntryUiControl;

        entryControl.SongQueueEntryDto = null;
        entryControl.OnDelete = null;
        entryControl.OnToggleMedley = null;
    }

    private VisualElement OnMakeItem()
    {
        VisualElement visualElement = songQueueEntryUi.CloneTreeAndGetFirstChild();
        SongQueueEntryUiControl entryControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<SongQueueEntryUiControl>();
        visualElement.userData = entryControl;
        return visualElement;
    }

    public void SetSongQueueEntryDtos(IReadOnlyList<SongQueueEntryDto> songQueueEntryDtos)
    {
        this.songQueueEntryDtos = songQueueEntryDtos;
        listView.itemsSource = this.songQueueEntryDtos.ToList();
    }

    public int GetSongQueueEntryIndex(SongQueueEntryDto songQueueEntryDto)
    {
        return songQueueEntryDtos.IndexOf(songQueueEntryDto);
    }

    public void Clear()
    {
        SetSongQueueEntryDtos(new List<SongQueueEntryDto>());
    }

    public class ItemIndexChangedEvent
    {
        public int OldIndex { get; private set; }
        public int NewIndex { get; private set; }
        public IReadOnlyList<SongQueueEntryDto> UpdatedItems { get; private set; }

        public ItemIndexChangedEvent(int oldIndex, int newIndex, IReadOnlyList<SongQueueEntryDto> updatedItems)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            UpdatedItems = updatedItems;
        }
    }
}
