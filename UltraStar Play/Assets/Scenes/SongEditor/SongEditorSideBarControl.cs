using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine.UIElements;
using UniRx;

public class SongEditorSideBarControl : INeedInjection, IInjectionFinishedListener
{
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

    [Inject]
    private Injector injector;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongEditorHistoryManager historyManager;

    [Inject]
    private SongEditorNoteRecorder songEditorNoteRecorder;

    private readonly TabGroupControl sideBarTabGroupControl = new TabGroupControl();

    public bool IsAnySideBarContainerVisible => sideBarTabGroupControl.IsAnyContainerVisible;

    public void OnInjectionFinished()
    {
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

        UpdateInputLegend();
        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        InitTabGroup();
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
        helpSideBarContainer.Children()
            .Where(it => it != helpTitle)
            .ToList()
            .ForEach(it => it.RemoveFromHierarchy());

        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
            TranslationManager.GetTranslation(R.Messages.back),
            helpSideBarContainer);

        List<InputActionInfo> inputActionInfos = new List<InputActionInfo>();
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
            inputActionInfos.Add(new InputActionInfo("Select Next Note", "6 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Select Previous Note", "4 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Play Selected Notes", "5 (Numpad)"));
            inputActionInfos.Add(new InputActionInfo("Toggle Play / Pause", "Double Click"));
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

}
