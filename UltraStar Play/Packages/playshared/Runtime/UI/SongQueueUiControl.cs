using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(songQueueEntryUi))]
    private VisualTreeAsset songQueueEntryUi;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntriesScrollView)]
    private VisualElement songQueueEntriesScrollView;

    [Inject]
    private Injector injector;
    
    public List<SongQueueEntryUiControl> SongQueueEntryControls { get; private set; } = new();

    public Action<SongQueueEntryDto> OnDelete { get; set; }
    public Action<SongQueueEntryDto> OnToggleMedley { get; set; }

    public void OnInjectionFinished()
    {
        Clear();
    }
    
    public void SetSongQueueEntryDtos(IReadOnlyList<SongQueueEntryDto> songQueueEntryDtos)
    {
        songQueueEntriesScrollView.Clear();
        SongQueueEntryControls.Clear();
        
        songQueueEntryDtos.ForEach(songQueueEntryDto => CreateSongQueueEntryControl(songQueueEntryDto));

        if (SongQueueEntryControls.IsNullOrEmpty())
        {
            return;
        }
        
        // Hide medley button of first entry.
        SongQueueEntryControls.FirstOrDefault().HideToggleMedleyButton();
        
        // Remove borders of medley entries.
        SongQueueEntryUiControl lastSongQueueEntryControl = null;
        foreach (SongQueueEntryUiControl currentSongQueueEntryControl in SongQueueEntryControls)
        {
            if (lastSongQueueEntryControl != null
                && currentSongQueueEntryControl.SongQueueEntryDto.IsMedleyWithPreviousEntry)
            {
                lastSongQueueEntryControl.VisualElement.AddToClassList("medleyWithNext");
                currentSongQueueEntryControl.VisualElement.AddToClassList("medleyWithPrevious");
            }
            lastSongQueueEntryControl = currentSongQueueEntryControl;
        }
    }
    
    private void CreateSongQueueEntryControl(SongQueueEntryDto songQueueEntryDto)
    {
        VisualElement songQueueEntry = songQueueEntryUi.CloneTreeAndGetFirstChild();
        songQueueEntriesScrollView.Add(songQueueEntry);

        SongQueueEntryUiControl songQueueEntryControl = injector
            .WithRootVisualElement(songQueueEntry)
            .WithBindingForInstance(songQueueEntryDto)
            .CreateAndInject<SongQueueEntryUiControl>();
        songQueueEntryControl.OnDelete = () => OnDelete?.Invoke(songQueueEntryDto);
        songQueueEntryControl.OnToggleMedley = () => OnToggleMedley?.Invoke(songQueueEntryDto);
        
        SongQueueEntryControls.Add(songQueueEntryControl);
    }

    public void HideControls()
    {
        SongQueueEntryControls.ForEach(it => it.HideControls());
    }

    public void Clear()
    {
        SetSongQueueEntryDtos(new List<SongQueueEntryDto>());
    }
}
