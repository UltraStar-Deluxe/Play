using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(songQueueEntryUi))]
    private VisualTreeAsset songQueueEntryUi;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntriesListView)]
    private ListView songQueueEntriesListView;

    [Inject]
    private Injector injector;

    private List<SongQueueEntryDto> songQueueEntryDtos = new List<SongQueueEntryDto>();

    public Action<SongQueueEntryDto> OnDelete { get; set; }
    public Action<SongQueueEntryDto> OnToggleMedley { get; set; }

    public void OnInjectionFinished()
    {
        songQueueEntriesListView.makeItem = OnMakeItem;
        songQueueEntriesListView.bindItem = OnBindItem;
        songQueueEntriesListView.unbindItem = OnUnbindItem;
        songQueueEntriesListView.handleDrop += OnHandleDrop;

        Clear();
    }

    private UnityEngine.UIElements.DragVisualMode OnHandleDrop(HandleDragAndDropArgs arg)
    {
        Debug.Log("OnHandleDrop");
        return UnityEngine.UIElements.DragVisualMode.Move;
    }

    private void OnBindItem(VisualElement element, int index)
    {
        SongQueueEntryUiControl entryControl = element.userData as SongQueueEntryUiControl;
        SongQueueEntryDto songQueueEntryDto = songQueueEntryDtos[index];

        // TODO: implement
        // entryControl.SongQueueEntryDto = songQueueEntryDto;
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
        if (songQueueEntryDto.IsMedleyWithPreviousEntry)
        {
            element.AddToClassList("medleyWithPrevious");
        }

        SongQueueEntryDto nextSongQueueEntryDto = CollectionUtils.SafeGet(songQueueEntryDtos, index + 1, null);
        if (nextSongQueueEntryDto != null
            && nextSongQueueEntryDto.IsMedleyWithPreviousEntry)
        {
            element.AddToClassList("medleyWithNext");
        }
    }

    private void OnUnbindItem(VisualElement element, int index)
    {
        SongQueueEntryUiControl entryControl = element.userData as SongQueueEntryUiControl;

        // TODO: implement
        // entryControl.SongQueueEntryDto = null;
        entryControl.OnDelete = () => { };
        entryControl.OnToggleMedley = () => { };
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
        // VisualElement focusedElement = VisualElementUtils.GetFocusedVisualElement(songQueueEntriesListView.focusController);
        // if (focusedElement != null
        //     && focusedElement.GetAncestors().Contains(songQueueEntriesListView))
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

        songQueueEntriesListView.itemsSource = this.songQueueEntryDtos;

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
