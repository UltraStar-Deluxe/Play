using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorAiToolbarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.vocalsIsolationButton)]
    private Button vocalsIsolationButton;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionButton)]
    private Button pitchDetectionButton;

    [Inject(UxmlName = R.UxmlNames.lyricsAlignmentButton)]
    private Button lyricsAlignmentButton;

    [Inject(UxmlName = R.UxmlNames.toggleAiToolbarButton)]
    private Button toggleAiToolbarButton;

    [Inject(UxmlName = R.UxmlNames.workflowContainer)]
    private VisualElement workflowContainer;

    [Inject(UxmlName = R.UxmlNames.shrinkIcon)]
    private VisualElement shrinkIcon;

    [Inject(UxmlName = R.UxmlNames.expandIcon)]
    private VisualElement expandIcon;

    [Inject(UxmlName = R.UxmlNames.vocalsIsolationStepIndicator)]
    private VisualElement vocalsIsolationStepIndicator;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionStepIndicator)]
    private VisualElement pitchDetectionStepIndicator;

    [Inject(UxmlName = R.UxmlNames.lyricsAlignmentStepIndicator)]
    private VisualElement lyricsAlignmentStepIndicator;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private PitchDetectionAction pitchDetectionAction;

    [Inject]
    private ForcedAlignmentAction forcedAlignmentAction;

    [Inject]
    private SongEditorPitchDetectionControl songEditorPitchDetectionControl;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;
    
    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private ForcedAlignmentManager forcedAlignmentManager;

    [Inject]
    private DialogManager dialogManager;

    private bool isExpanded = true;

    public void OnInjectionFinished()
    {
        vocalsIsolationButton.RegisterCallbackButtonTriggered(_ => OnVocalsIsolationButtonClicked());
        pitchDetectionButton.RegisterCallbackButtonTriggered(_ => pitchDetectionAction.CreateNotesUsingAi(true));
        lyricsAlignmentButton.RegisterCallbackButtonTriggered(_ =>
        {
            SongEditorForcedAlignmentUtils.ShowForcedAlignmentDialog(
                songEditorSceneControl,
                forcedAlignmentAction,
                SongMetaUtils.GetLyrics(songMeta.Voices.FirstOrDefault()));
        });

        toggleAiToolbarButton.RegisterCallbackButtonTriggered(_ => ToggleExpanded());

        songEditorPitchDetectionControl.PitchDetectionFinishedEventStream
            .Subscribe(_ => UpdateIndicators())
            .AddTo(gameObject);
        
        audioSeparationManager.AudioSeparationFinishedEventStream
            .Subscribe(_ => UpdateIndicators())
            .AddTo(gameObject);

        forcedAlignmentManager.ForcedAlignmentFinishedEventStream
            .Subscribe(_ => UpdateIndicators())
            .AddTo(gameObject);

        UpdateIndicators();
        UpdateSizeToggle();
    }

    private async void OnVocalsIsolationButtonClicked()
    {
        await SongEditorAudioSeparationUtils.AskToPerformAudioSeparation(songMeta, audioSeparationManager, dialogManager);
    }

    private void ToggleExpanded()
    {
        isExpanded = !isExpanded;
        UpdateSizeToggle();
    }

    private void UpdateSizeToggle()
    {
        workflowContainer.SetVisibleByDisplay(isExpanded);
        shrinkIcon.SetVisibleByVisibility(isExpanded);
        expandIcon.SetVisibleByVisibility(!isExpanded);
    }

    private void UpdateIndicators()
    {
        bool hasPitchDetectionData = songEditorPitchDetectionControl.LastPitchDetectionResult != null;
        pitchDetectionStepIndicator.Q(R.UxmlNames.checkIcon).SetVisibleByVisibility(hasPitchDetectionData);

        bool hasVocalsAndInstrumental = SongMetaUtils.VocalsAudioResourceExists(songMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(songMeta);
        vocalsIsolationStepIndicator.Q(R.UxmlNames.checkIcon).SetVisibleByVisibility(hasVocalsAndInstrumental);

        bool hasNotesOrLyrics = SongMetaUtils.GetAllNotes(songMeta).Any();
        lyricsAlignmentStepIndicator.Q(R.UxmlNames.checkIcon).SetVisibleByVisibility(hasNotesOrLyrics);
    }
}
