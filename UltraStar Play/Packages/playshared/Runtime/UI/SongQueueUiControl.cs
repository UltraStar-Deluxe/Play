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

    [Inject(UxmlName = R_PlayShared.UxmlNames.addButton)]
    private Button addButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.addAsMedleyButton)]
    private Button addAsMedleyButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntriesScrollView)]
    private VisualElement songQueueEntriesScrollView;

    [Inject]
    private Injector injector;
    
    private readonly List<SongQueueEntryUiControl> songQueueEntryControls = new();

    public Action OnAdd { get; set; }
    public Action OnAddAsMedley { get; set; }
    public Action<SongQueueEntryDto> OnDelete { get; set; }
    public Action<SongQueueEntryDto> OnToggleMedley { get; set; }

    public void OnInjectionFinished()
    {
        addButton.RegisterCallbackButtonTriggered(() => OnAdd?.Invoke());
        addAsMedleyButton.RegisterCallbackButtonTriggered(() => OnAddAsMedley?.Invoke());

        SetSongQueueEntryDtos(new List<SongQueueEntryDto>());
    }
    
    public void SetSongQueueEntryDtos(IReadOnlyList<SongQueueEntryDto> songQueueEntryDtos)
    {
        songQueueEntriesScrollView.Clear();
        songQueueEntryControls.Clear();
        songQueueEntryDtos.ForEach(songQueueEntryDto => CreateSongQueueEntryControl(songQueueEntryDto));

        if (songQueueEntryControls.IsNullOrEmpty())
        {
            return;
        }
        
        // Hide medley button of first entry.
        songQueueEntryControls.FirstOrDefault().HideToggleMedleyButton();
        
        // Remove borders of medley entries.
        SongQueueEntryUiControl lastSongQueueEntryControl = null;
        foreach (SongQueueEntryUiControl currentSongQueueEntryControl in songQueueEntryControls)
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
        songQueueEntryControls.Add(songQueueEntryControl);
    }
}
