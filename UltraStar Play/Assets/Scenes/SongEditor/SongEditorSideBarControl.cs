﻿using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Inject(UxmlName = R.UxmlNames.undoButton)]
    private Button undoButton;

    [Inject(UxmlName = R.UxmlNames.redoButton)]
    private Button redoButton;

    [Inject(UxmlName = R.UxmlNames.exitSceneButton)]
    private Button exitSceneButton;

    [Inject(UxmlName = R.UxmlNames.saveButton)]
    private Button saveButton;

    [Inject(UxmlName = R.UxmlNames.helpTitle)]
    private VisualElement helpTitle;

    [Inject(UxmlName = R.UxmlNames.helpSideBarContainer)]
    private VisualElement helpSideBarContainer;

    [Inject(UxmlName = R.UxmlNames.toggleHelpButton)]
    private Button toggleHelpButton;

    [Inject(UxmlName = R.UxmlNames.issuesSideBarContainer)]
    private VisualElement issuesSideBarContainer;

    [Inject(UxmlName = R.UxmlNames.toggleIssuesButton)]
    private Button toggleIssuesButton;

    [Inject(UxmlName = R.UxmlNames.songPropertiesSideBarContainer)]
    private VisualElement songPropertiesSideBarContainer;

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

    [Inject(UxmlName = R.UxmlNames.playIcon)]
    private VisualElement playIcon;

    [Inject(UxmlName = R.UxmlNames.pauseIcon)]
    private VisualElement pauseIcon;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongEditorHistoryManager historyManager;

    [Inject]
    private SongEditorNoteRecorder songEditorNoteRecorder;

    [Inject]
    private SongEditorIssueAnalyzerControl issueAnalyzerControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private GameObject gameObject;

    private readonly TabGroupControl sideBarTabGroupControl = new();
    private readonly SongEditorSideBarPropertiesControl propertiesControl = new();
    private readonly SongEditorSideBarLayersControl sideBarLayersControl = new();
    private readonly SongEditorSideBarSettingsControl sideBarSettingsControl = new();

    public bool IsAnySideBarContainerVisible => sideBarTabGroupControl.IsAnyContainerVisible;

    private IReadOnlyCollection<SongIssue> lastIssues;

    public void OnInjectionFinished()
    {
        injector.Inject(propertiesControl);
        injector.Inject(sideBarLayersControl);
        injector.Inject(sideBarSettingsControl);

        togglePlaybackButton.RegisterCallbackButtonTriggered(() => songEditorSceneControl.ToggleAudioPlayPause());
        toggleRecordingButton.RegisterCallbackButtonTriggered(() =>
        {
            songEditorNoteRecorder.IsRecordingEnabled = !songEditorNoteRecorder.IsRecordingEnabled;
            if (songEditorNoteRecorder.IsRecordingEnabled)
            {
                toggleRecordingButton.AddToClassList("recording");
            }
            else
            {
                toggleRecordingButton.RemoveFromClassList("recording");
            }
        });
        undoButton.RegisterCallbackButtonTriggered(() => historyManager.Undo());
        redoButton.RegisterCallbackButtonTriggered(() => historyManager.Redo());
        exitSceneButton.RegisterCallbackButtonTriggered(() => songEditorSceneControl.ReturnToLastScene());
        saveButton.RegisterCallbackButtonTriggered(() => songEditorSceneControl.SaveSong());

        // Hide save button if AutoSave is enabled
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.AutoSave)
            .Subscribe(autoSave => saveButton.SetVisibleByDisplay(!autoSave))
            .AddTo(gameObject);

        UpdateInputLegend();
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

        toggleSideBarSizeButton.RegisterCallbackButtonTriggered(() =>
            settings.SongEditorSettings.SmallLeftSideBar = !settings.SongEditorSettings.SmallLeftSideBar);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.SmallLeftSideBar)
            .Subscribe(newValue => UpdateLeftSideBarClasses())
            .AddTo(gameObject);
        UpdateLeftSideBarClasses();

        songAudioPlayer.PlaybackStartedEventStream
            .Subscribe(_ => ShowPauseIcon());
        songAudioPlayer.PlaybackStoppedEventStream
            .Subscribe(_ => ShowPlayIcon());

        InitTabGroup();
    }

    private void ShowPlayIcon()
    {
        playIcon.ShowByDisplay();
        pauseIcon.HideByDisplay();
    }

    private void ShowPauseIcon()
    {
        playIcon.HideByDisplay();
        pauseIcon.ShowByDisplay();
    }

    private void UpdateLeftSideBarClasses()
    {
        if (settings.SongEditorSettings.SmallLeftSideBar)
        {
            leftSideBarMainColumn.AddToClassList("small");
        }
        else
        {
            leftSideBarMainColumn.RemoveFromClassList("small");
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

        float issueStartPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, issue.StartBeat);
        int issueStartPositionInSeconds = (int)(issueStartPositionInMillis / 1000);
        visualElement.Q<Button>(R.UxmlNames.goToIssueButton).RegisterCallbackButtonTriggered(() => GoToIssue(issue));
        visualElement.Q<Label>(R.UxmlNames.issueMessageLabel).text = issue.Message;
        visualElement.Q<Label>(R.UxmlNames.issuePositionLabel).text = $"({issueStartPositionInSeconds}s)";
        visualElement.Q<VisualElement>(R.UxmlNames.issueImage).style.unityBackgroundImageTintColor = SongIssueUtils.GetColorForIssue(issue);
    }

    private void GoToIssue(SongIssue issue)
    {
        float issueStartPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, issue.StartBeat);
        songAudioPlayer.PositionInSongInMillis = issueStartPositionInMillis;
    }

    private void InitTabGroup()
    {
        sideBarTabGroupControl.AllowNoContainerVisible = true;
        sideBarTabGroupControl.AddTabGroupButton(toggleIssuesButton, issuesSideBarContainer);
        sideBarTabGroupControl.AddTabGroupButton(toggleHelpButton, helpSideBarContainer);
        sideBarTabGroupControl.AddTabGroupButton(toggleSongPropertiesButton, songPropertiesSideBarContainer);
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
        ClearSideBar(helpSideBarContainer);

        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
            TranslationManager.GetTranslation(R.Messages.back),
            helpSideBarContainer);

        List<InputActionInfo> inputActionInfos = new();
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
        }
        else if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        {
            inputActionInfos.Add(new InputActionInfo("Zoom", "2 Finger Pinch Gesture"));
            inputActionInfos.Add(new InputActionInfo("Scroll", "2 Finger Drag"));
            inputActionInfos.Add(new InputActionInfo("Toggle Play Pause", "Double Tap"));
            inputActionInfos.Add(new InputActionInfo(TranslationManager.GetTranslation(R.Messages.action_openContextMenu),
                TranslationManager.GetTranslation(R.Messages.action_longPress)));
        }

        inputActionInfos.ForEach(inputActionInfo => helpSideBarContainer.Add(InputLegendControl.CreateInputActionInfoUi(inputActionInfo)));
    }

    private void ClearSideBar(VisualElement visualElement)
    {
        visualElement.Children()
            .Where(it => !it.ClassListContains("secondarySideBarTitle"))
            .ToList()
            .ForEach(it => it.RemoveFromHierarchy());
    }
}
