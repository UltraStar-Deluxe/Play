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

    private ListViewReorderByDragAndDropControl listViewReorderByDragAndDropControl;

    private IReadOnlyList<SongQueueEntryDto> songQueueEntryDtos = new List<SongQueueEntryDto>();

    public Action<SongQueueEntryDto> OnDelete { get; set; }
    public Action<SongQueueEntryDto> OnToggleMedley { get; set; }
    public Action<List<SongQueueEntryDto>> OnReorderedList { get; set; }

    public void OnInjectionFinished()
    {
        listView.makeItem = OnMakeItem;
        listView.bindItem = OnBindItem;
        listView.unbindItem = OnUnbindItem;

        listViewReorderByDragAndDropControl = new ListViewReorderByDragAndDropControl(listView);
        listViewReorderByDragAndDropControl.ReorderEventStream
            .Subscribe(evt =>
            {
                OnReorderedList?.Invoke(evt.UpdatedItemsSource as List<SongQueueEntryDto>);
            });

        Clear();
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
        // Remember focus
        // int focusedIndex = -1;
        // bool wasToggleMedleyButtonFocused = false;
        // VisualElement focusedElement = VisualElementUtils.GetFocusedVisualElement(listView.focusController);
        // if (focusedElement != null
        //     && focusedElement.GetAncestors().Contains(listView))
        // {
        //     // Search index of focused element
        //     SongQueueEntryUiControl focusedSongQueueEntryUiControl = SongQueueEntryControls.FirstOrDefault(control => focusedElement.GetAncestors().Contains(control.VisualElement));
        //     if (focusedSongQueueEntryUiControl != null)
        //     {
        //         focusedIndex = SongQueueEntryControls.IndexOf(focusedSongQueueEntryUiControl);
        //         if (focusedIndex >= 0)
        //         {
        //             wasToggleMedleyButtonFocused = focusedElement.name == R_PlayShared.UxmlNames.toggleMedleyButton;
        //         }
        //     }
        // }

        this.songQueueEntryDtos = songQueueEntryDtos;
        listView.itemsSource = this.songQueueEntryDtos.ToList();

        // Restore focus
        // focusedIndex = Math.Min(focusedIndex, SongQueueEntryControls.Count - 1);
        // if (focusedIndex >= 0)
        // {
        //     SongQueueEntryUiControl focusedSongQueueEntryUiControl = SongQueueEntryControls[focusedIndex];
        //     if (focusedSongQueueEntryUiControl != null)
        //     {
        //         if (wasToggleMedleyButtonFocused)
        //         {
        //             focusedSongQueueEntryUiControl.ToggleMedleyButton?.Focus();
        //         }
        //         else
        //         {
        //             focusedSongQueueEntryUiControl.DeleteButton?.Focus();
        //         }
        //     }
        // }
    }

    public void Clear()
    {
        SetSongQueueEntryDtos(new List<SongQueueEntryDto>());
    }
}
