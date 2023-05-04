using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Vosk;

public class SongEditorSideBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(issueSideBarEntryUi))]
    private VisualTreeAsset issueSideBarEntryUi;

    [Inject(UxmlName = R.UxmlNames.leftSideBarMainColumn)]
    private VisualElement leftSideBarMainColumn;

    [Inject(UxmlName = R.UxmlNames.togglePlaybackButton)]
    private Button togglePlaybackButton;

    [Inject(UxmlName = R.UxmlNames.toggleRecordingButton)]
    private Button toggleRecordingButton;

    [Inject(UxmlName = R.UxmlNames.doPitchDetectionButton)]
    private Button doPitchDetectionButton;
    
    [Inject(UxmlName = R.UxmlNames.doSpeechRecognitionButton)]
    private Button doSpeechRecognitionButton;
    
    [Inject(UxmlName = R.UxmlNames.undoButton)]
    private Button undoButton;

    [Inject(UxmlName = R.UxmlNames.redoButton)]
    private Button redoButton;

    [Inject(UxmlName = R.UxmlNames.exitSceneButton)]
    private Button exitSceneButton;

    [Inject(UxmlName = R.UxmlNames.saveButton)]
    private Button saveButton;

    [Inject(UxmlName = R.UxmlNames.toggleHelpButton)]
    private Button toggleHelpButton;

    [Inject(UxmlName = R.UxmlNames.issuesSideBarContainer)]
    private VisualElement issuesSideBarContainer;

    [Inject(UxmlName = R.UxmlNames.toggleIssuesButton)]
    private Button toggleIssuesButton;

    [Inject(UxmlName = R.UxmlNames.sideBarSongPropertiesUi)]
    private VisualElement sideBarSongPropertiesUi;

    [Inject(UxmlName = R.UxmlNames.toggleSongPropertiesButton)]
    private Button toggleSongPropertiesButton;

    [Inject(UxmlName = R.UxmlNames.layersSideBarContainer)]
    private VisualElement layersSideBarContainer;

    [Inject(UxmlName = R.UxmlNames.toggleLayersButton)]
    private Button toggleLayersButton;

    [Inject(UxmlName = R.UxmlNames.settingsSideBarContainer)]
    private VisualElement settingsSideBarContainer;

    [Inject(UxmlName = R.UxmlNames.toggleSettingsButton)]
    private Button toggleSettingsButton;

    [Inject(UxmlName = R.UxmlNames.toggleSideBarSizeButton)]
    private Button toggleSideBarSizeButton;

    [Inject(UxmlName = R.UxmlNames.openSongFolderButton)]
    private Button openSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.playIcon)]
    private VisualElement playIcon;

    [Inject(UxmlName = R.UxmlNames.pauseIcon)]
    private VisualElement pauseIcon;
    
    [Inject(UxmlName = R.UxmlNames.sideBarSecondaryColumnUi)]
    private VisualElement sideBarSecondaryColumnUi;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;
    
    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongEditorHistoryManager historyManager;

    [Inject]
    private SongEditorIssueAnalyzerControl issueAnalyzerControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private PitchDetectionAction pitchDetectionAction;
    
    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;
    
    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;
    
    [Inject]
    private UiManager uiManager;
    
    private readonly TabGroupControl sideBarTabGroupControl = new();
    private readonly SongEditorSideBarPropertiesControl propertiesControl = new();
    private readonly SongEditorSideBarLayersControl sideBarLayersControl = new();
    private readonly SongEditorSideBarSettingsControl sideBarSettingsControl = new();

    public bool IsAnySideBarContainerVisible => sideBarTabGroupControl.IsAnyContainerVisible;

    private IReadOnlyCollection<SongIssue> lastIssues;

    private MessageDialogControl helpDialogControl;
    private VisualElement inputLegendContainer;

    public void OnInjectionFinished()
    {
        injector.Inject(propertiesControl);
        injector.Inject(sideBarLayersControl);
        injector.Inject(sideBarSettingsControl);

        sideBarSecondaryColumnUi.ShowByDisplay();
        
        if (PlatformUtils.IsStandalone)
        {
            openSongFolderButton.RegisterCallbackButtonTriggered(_ => SongMetaUtils.OpenDirectory(songMeta));
        }
        else
        {
            openSongFolderButton.HideByDisplay();
        }

        togglePlaybackButton.RegisterCallbackButtonTriggered(_ => songEditorSceneControl.ToggleAudioPlayPause());
        toggleRecordingButton.RegisterCallbackButtonTriggered(_ =>
        {
            nonPersistentSettings.IsSongEditorRecordingEnabled.Value = !nonPersistentSettings.IsSongEditorRecordingEnabled.Value;
            UpdateRecordingButton();
        });
        UpdateRecordingButton();
        
        doPitchDetectionButton.RegisterCallbackButtonTriggered(_ => DoDetectPitch());
        doSpeechRecognitionButton.RegisterCallbackButtonTriggered(_ => DoSpeechRecognition());
        
        undoButton.RegisterCallbackButtonTriggered(_ => historyManager.Undo());
        redoButton.RegisterCallbackButtonTriggered(_ => historyManager.Redo());
        exitSceneButton.RegisterCallbackButtonTriggered(_ => songEditorSceneControl.ReturnToLastScene());
        saveButton.RegisterCallbackButtonTriggered(_ => songMetaManager.SaveSong(songMeta, false));

        // Hide save button if AutoSave is enabled
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.AutoSave)
            .Subscribe(autoSave => saveButton.SetVisibleByDisplay(!autoSave))
            .AddTo(gameObject);
        
        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        issuesSideBarContainer.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (issuesSideBarContainer.style.display == new StyleEnum<DisplayStyle>(DisplayStyle.Flex))
            {
                UpdateIssueSideBar(lastIssues);
            }
        });
        issueAnalyzerControl.IssuesEventStream
            .Subscribe(issues => UpdateIssueSideBar(issues));

        toggleSideBarSizeButton.RegisterCallbackButtonTriggered(_ =>
            settings.SongEditorSettings.SmallLeftSideBar = !settings.SongEditorSettings.SmallLeftSideBar);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.SmallLeftSideBar)
            .Subscribe(newValue =>
            {
                UpdatePlayPauseIcon();
                UpdateLeftSideBarClasses();
            })
            .AddTo(gameObject);
        UpdateLeftSideBarClasses();
        UpdatePlayPauseIcon();

        toggleHelpButton.RegisterCallbackButtonTriggered(_ => ShowSongEditorHelpDialog());
        
        songAudioPlayer.PlaybackStartedEventStream
            .Subscribe(_ => UpdatePlayPauseIcon());
        songAudioPlayer.PlaybackStoppedEventStream
            .Subscribe(_ => UpdatePlayPauseIcon());

        InitTabGroup();
    }

    private void ShowSongEditorHelpDialog()
    {
        if (helpDialogControl != null)
        {
            return;
        }
        
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_audioSeparation_title),
                TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_audioSeparation) },
            { TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_pitchDetection_title),
                TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_pitchDetection) },
            { TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_lyricsDictation_title),
                TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_lyricsDictation) },
            { TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_buttonTapping_title),
                TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_buttonTapping) },
            { TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_editingLyrics_title),
                TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_editingLyrics) },
            { TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_layers_title),
                TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_layers) },
        };
        helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.songEditor_helpDialog_title),
            titleToContentMap);
        helpDialogControl.DialogClosedEventStream.Subscribe(_ => helpDialogControl = null);

        // Add controls info
        inputLegendContainer = new();
        UpdateInputLegend();
        
        AccordionItem controlsAccordionItem = new AccordionItem("Controls");
        controlsAccordionItem.Add(inputLegendContainer);
        helpDialogControl.DialogRootVisualElement.Q<AccordionGroup>().Add(controlsAccordionItem);
        
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToSongEditor)));
        helpDialogControl.AddButton("Video Tutorials",
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_songEditorVideoTutorials)));
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(helpDialogControl.DialogRootVisualElement);
    }

    private void DoSpeechRecognition()
    {
        if (NoteAreaSelectionDragListener.LastSelectionRect == null
            || NoteAreaSelectionDragListener.LastSelectionRect.LengthInBeats <= 0)
        {
            return;
        }
        
        SpeechRecognitionParameters speechRecognitionParameters = speechRecognitionAction.CreateSpeechRecognizerParameters(settings.SongEditorSettings.SpeechRecognitionSamplesSource);
        VoskRecognizer speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);
        speechRecognitionAction.CreateNotesFromSpeechRecognition(
            NoteAreaSelectionDragListener.LastSelectionRect.MinBeat,
            NoteAreaSelectionDragListener.LastSelectionRect.LengthInBeats,
            settings.SongEditorSettings.SpeechRecognitionSamplesSource,
            2,
            true,
            speechRecognitionParameters,
            speechRecognizer,
            false);
    }

    private void DoDetectPitch()
    {
        if (NoteAreaSelectionDragListener.LastSelectionRect == null
            || NoteAreaSelectionDragListener.LastSelectionRect.LengthInBeats <= 0)
        {
            return;
        }
        
        pitchDetectionAction.CreateNotesForDetectedPitch(
            NoteAreaSelectionDragListener.LastSelectionRect.MinBeat,
            NoteAreaSelectionDragListener.LastSelectionRect.LengthInBeats,
            settings.SongEditorSettings.PitchDetectionSamplesSource,
            true);
    }

    private void UpdateRecordingButton()
    {
        if (nonPersistentSettings.IsSongEditorRecordingEnabled.Value)
        {
            toggleRecordingButton.AddToClassList("recording");
        }
        else
        {
            toggleRecordingButton.RemoveFromClassList("recording");
        }
    }

    private void UpdatePlayPauseIcon()
    {
        bool playIconVisible = !songAudioPlayer.IsPlaying;
        bool pauseIconVisible = songAudioPlayer.IsPlaying;
        playIcon.SetVisibleByDisplay(playIconVisible);
        pauseIcon.SetVisibleByDisplay(pauseIconVisible);
    }

    private void UpdateLeftSideBarClasses()
    {
        if (settings.SongEditorSettings.SmallLeftSideBar)
        {
            leftSideBarMainColumn.AddToClassList("small");
            leftSideBarMainColumn.RemoveFromClassList("wide");
        }
        else
        {
            leftSideBarMainColumn.RemoveFromClassList("small");
            leftSideBarMainColumn.AddToClassList("wide");
        }
    }

    private void UpdateIssueSideBar(IReadOnlyCollection<SongIssue> issues)
    {
        lastIssues = issues;

        ClearSideBar(issuesSideBarContainer);
        if (issues.IsNullOrEmpty()
            || !issuesSideBarContainer.IsVisibleByDisplay())
        {
            return;
        }
        issues.ForEach(issue => CreateSideBarIssueUi(issue));
    }

    private void CreateSideBarIssueUi(SongIssue issue)
    {
        VisualElement visualElement = issueSideBarEntryUi.CloneTree().Children().First();
        issuesSideBarContainer.Add(visualElement);

        double issueStartPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, issue.StartBeat);
        int issueStartPositionInSeconds = (int)(issueStartPositionInMillis / 1000);
        visualElement.Q<Button>(R.UxmlNames.goToIssueButton).RegisterCallbackButtonTriggered(_ => GoToIssue(issue));
        visualElement.Q<Label>(R.UxmlNames.issueMessageLabel).text = issue.Message;
        visualElement.Q<Label>(R.UxmlNames.issuePositionLabel).text = $"({issueStartPositionInSeconds}s)";
        
        VisualElement issueImage = visualElement.Q<VisualElement>(R.UxmlNames.issueImage);
        if (issue.Severity == ESongIssueSeverity.Error)
        {
            issueImage.AddToClassList("error");
        }
        else if (issue.Severity == ESongIssueSeverity.Warning)
        {
            issueImage.AddToClassList("warning");
        }
    }

    private void GoToIssue(SongIssue issue)
    {
        double issueStartPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, issue.StartBeat);
        songAudioPlayer.PositionInSongInMillis = issueStartPositionInMillis;
    }

    private void InitTabGroup()
    {
        sideBarTabGroupControl.AllowNoContainerVisible = true;
        sideBarTabGroupControl.AddTabGroupButton(toggleIssuesButton, issuesSideBarContainer);
        sideBarTabGroupControl.AddTabGroupButton(toggleSongPropertiesButton, sideBarSongPropertiesUi);
        sideBarTabGroupControl.AddTabGroupButton(toggleLayersButton, layersSideBarContainer);
        sideBarTabGroupControl.AddTabGroupButton(toggleSettingsButton, settingsSideBarContainer);
        sideBarTabGroupControl.HideAllContainers();
    }

    public void HideSideBarContainers()
    {
        sideBarTabGroupControl.HideAllContainers();
    }

    private void UpdateInputLegend()
    {
        if (inputLegendContainer == null)
        {
            return;
        }
        
        List<InputActionInfo> inputActionInfos = new();
        
        inputActionInfos.Add(InputLegendControl.GetInputActionInfo(R.InputActions.usplay_back, TranslationManager.GetTranslation(R.Messages.back)));

        if (inputManager.InputDeviceEnum == EInputDevice.KeyboardAndMouse)
        {
            inputActionInfos.Add(new InputActionInfo("Zoom Horizontal", "Ctrl+Mouse Wheel"));
            inputActionInfos.Add(new InputActionInfo("Zoom Vertical", "Ctrl+Shift+Mouse Wheel"));
            inputActionInfos.Add(new InputActionInfo("Scroll Horizontal", "Mouse Wheel | Arrow Keys | Middle Mouse Button+Drag"));
            inputActionInfos.Add(new InputActionInfo("Scroll Vertical", "Shift+Mouse Wheel | Shift+Arrow Keys | Middle Mouse Button+Drag"));
            inputActionInfos.Add(new InputActionInfo("Move Note Horizontal", "Shift+Arrow Keys | 1 (Numpad) | 3 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Move Note Vertical", "Shift+Arrow Keys | Minus (Numpad) | Plus (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Move Note Vertical One Octave", "Ctrl+Shift+Arrow Keys"));
            inputActionInfos.Add(new InputActionInfo("Move Left Side Of Note", "Ctrl+Arrow Keys | Divide (Numpad) | Multiply (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Move Right side Of Note", "Alt+Arrow Keys | 7 (Numpad) | 8 (Numpad) | 9 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Select Next Note", "Tab | 6 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Select Previous Note", "Shift+Tab | 4 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Play Selected Notes", "Ctrl+Space | 5 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Toggle Play / Pause", "Space | Double Click"));
            inputActionInfos.Add(new InputActionInfo("Play MIDI Sound Of Note", "Ctrl+Click Note"));
            inputActionInfos.Add(new InputActionInfo("Draw new Note", "Shift+Drag (no selection)"));
            inputActionInfos.Add(new InputActionInfo("Extend Selection", "Shift+Drag (with existing selection)"));
            inputActionInfos.Add(new InputActionInfo("Toggle Selection", "Ctrl+Drag"));
        }
        else if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        {
            inputActionInfos.Add(new InputActionInfo("Zoom", "2 Finger Pinch Gesture"));
            inputActionInfos.Add(new InputActionInfo("Scroll", "2 Finger Drag"));
            inputActionInfos.Add(new InputActionInfo("Toggle Play Pause", "Double Tap"));
            inputActionInfos.Add(new InputActionInfo(TranslationManager.GetTranslation(R.Messages.action_openContextMenu),
                TranslationManager.GetTranslation(R.Messages.action_longPress)));
        }

        inputLegendContainer.Clear();
        inputActionInfos.ForEach(inputActionInfo =>
            inputLegendContainer.Add(InputLegendControl.CreateInputActionInfoUi(inputActionInfo)));
    }

    private void ClearSideBar(VisualElement visualElement)
    {
        visualElement.Children()
            .Where(it => !it.ClassListContains("secondarySideBarTitle"))
            .ToList()
            .ForEach(it => it.RemoveFromHierarchy());
    }
}
