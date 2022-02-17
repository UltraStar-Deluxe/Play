using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorSceneControl : MonoBehaviour, IBinder, INeedInjection, IInjectionFinishedListener
{
    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongEditorNoteRecorder songEditorNoteRecorder;

    [InjectedInInspector]
    public SongEditorSelectionControl selectionControl;

    [InjectedInInspector]
    public EditorNoteDisplayer editorNoteDisplayer;

    [InjectedInInspector]
    public MicPitchTracker micPitchTracker;

    [InjectedInInspector]
    public GraphicRaycaster graphicRaycaster;

    [InjectedInInspector]
    public SongEditorHistoryManager historyManager;

    [InjectedInInspector]
    public SongEditorLayerManager songEditorLayerManager;

    [InjectedInInspector]
    public SongEditorMidiFileImporter midiFileImporter;

    [InjectedInInspector]
    public SongEditorCopyPasteManager songEditorCopyPasteManager;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

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

    [Inject(UxmlName = R.UxmlNames.statusBarSongInfoLabel)]
    private Label statusBarSongInfoLabel;

    [Inject(UxmlName = R.UxmlNames.editLyricsPopup)]
    private VisualElement editLyricsPopup;

    [Inject(UxmlName = R.UxmlNames.toggleHelpButton)]
    private Button toggleHelpButton;

    [Inject(UxmlName = R.UxmlNames.closeHelpOverlayButton)]
    private Button closeHelpOverlayButton;

    [Inject(UxmlName = R.UxmlNames.helpOverlay)]
    private VisualElement helpOverlay;

    [Inject(UxmlName = R.UxmlNames.helpContainer)]
    private VisualElement helpContainer;

    private readonly SongMetaChangeEventStream songMetaChangeEventStream = new SongMetaChangeEventStream();

    private double positionInSongInMillisWhenPlaybackStarted;

    private readonly Dictionary<Voice, Color> voiceToColorMap = new Dictionary<Voice, Color>();

    private bool audioWaveFormInitialized;

    public double StopPlaybackAfterPositionInSongInMillis { get; set; }

    private readonly OverviewAreaControl overviewAreaControl = new OverviewAreaControl();
    private readonly VideoAreaControl videoAreaControl = new VideoAreaControl();
    private readonly SongEditorVirtualPianoControl songEditorVirtualPianoControl = new SongEditorVirtualPianoControl();
    private readonly LyricsAreaControl lyricsAreaControl = new LyricsAreaControl();
    private readonly NoteAreaControl noteAreaControl = new NoteAreaControl();

    public SongMeta SongMeta
    {
        get
        {
            return SceneData.SelectedSongMeta;
        }
    }

    private SongEditorSceneData sceneData;
    public SongEditorSceneData SceneData
    {
        get
        {
            if (sceneData == null)
            {
                sceneData = SceneNavigator.Instance.GetSceneDataOrThrow<SongEditorSceneData>();
            }
            return sceneData;
        }
    }

    public bool IsHelpVisible => helpOverlay.IsVisibleByDisplay();

    private void Awake()
    {
        Debug.Log($"Start editing of '{SceneData.SelectedSongMeta.Title}' at {SceneData.PositionInSongInMillis} ms.");

        songAudioPlayer.Init(SongMeta);
        songVideoPlayer.SongMeta = SongMeta;

        songAudioPlayer.PositionInSongInMillis = SceneData.PositionInSongInMillis;
    }

    public void OnInjectionFinished()
    {
        injector.Inject(overviewAreaControl);
        injector.Inject(videoAreaControl);
        injector.Inject(songEditorVirtualPianoControl);
        injector.Inject(lyricsAreaControl);
        injector.Inject(noteAreaControl);
    }

    private void Start()
    {
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(OnAudioPlaybackStarted);
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(OnAudioPlaybackStopped);

        togglePlaybackButton.RegisterCallbackButtonTriggered(() => ToggleAudioPlayPause());
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
        exitSceneButton.RegisterCallbackButtonTriggered(() => ReturnToLastScene());
        undoButton.RegisterCallbackButtonTriggered(() => historyManager.Undo());
        redoButton.RegisterCallbackButtonTriggered(() => historyManager.Redo());
        toggleHelpButton.RegisterCallbackButtonTriggered(() => ToggleHelp());
        closeHelpOverlayButton.RegisterCallbackButtonTriggered(() => CloseHelp());
        saveButton.RegisterCallbackButtonTriggered(() => SaveSong());
        statusBarSongInfoLabel.text = $"{SongMeta.Artist} - {SongMeta.Title}";

        CloseHelp();
        HideEditLyricsPopup();

        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        if (uiDocument.rootVisualElement.focusController.focusedElement != null)
        {
            uiDocument.rootVisualElement.focusController.focusedElement.Blur();
        }
    }

    public void CloseHelp()
    {
        helpOverlay.HideByDisplay();
    }

    public void ToggleHelp()
    {
        if (helpOverlay.IsVisibleByDisplay())
        {
            CloseHelp();
        }
        else
        {
            helpOverlay.ShowByDisplay();
            UpdateInputLegend();
        }
    }

    private void UpdateInputLegend()
    {
        helpContainer.Clear();

        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
            TranslationManager.GetTranslation(R.Messages.back),
            helpContainer);

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

        inputActionInfos.ForEach(inputActionInfo => helpContainer.Add(InputLegendControl.CreateInputActionInfoUi(inputActionInfo)));
    }

    public void HideEditLyricsPopup()
    {
        // There is an exception in Unity code when the TextField becomes visible later (via display property).
        // Thus, keep it always "visible", but outside of the screen area.
        editLyricsPopup.style.top = -1000;
    }

    private void Update()
    {
        // Automatically stop playback after a given threshold (e.g. only play the selected notes)
        if (songAudioPlayer.IsPlaying
            && StopPlaybackAfterPositionInSongInMillis > 0
            && songAudioPlayer.PositionInSongInMillis > StopPlaybackAfterPositionInSongInMillis)
        {
            songAudioPlayer.PauseAudio();
            StopPlaybackAfterPositionInSongInMillis = 0;
        }

        lyricsAreaControl.Update();

        noteAreaControl.Update();
    }

    private void OnAudioPlaybackStopped(double positionInSongInMillis)
    {
        // Jump to last position in song when playback stops
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillisWhenPlaybackStarted;
    }

    private void OnAudioPlaybackStarted(double positionInSongInMillis)
    {
        positionInSongInMillisWhenPlaybackStarted = positionInSongInMillis;
    }

    public Color GetColorForVoice(Voice voice)
    {
        if (voiceToColorMap.TryGetValue(voice, out Color color))
        {
            return color;
        }
        else
        {
            // Define colors for the voices.
            CreateVoiceToColorMap();
            return voiceToColorMap[voice];
        }
    }

    // Returns the notes in the song as well as the notes in the layers in no particular order.
    public List<Note> GetAllNotes()
    {
        List<Note> result = new List<Note>();
        List<Note> notesInVoices = SongMetaUtils.GetAllNotes(SongMeta);
        List<Note> notesInLayers = songEditorLayerManager.GetAllNotes();
        result.AddRange(notesInVoices);
        result.AddRange(notesInLayers);
        return result;
    }
    
    public List<Note> GetAllVisibleNotes()
    {
        List<Note> result = new List<Note>();
        List<Note> notesInVoices = SongMetaUtils.GetAllNotes(SongMeta)
            .Where(note => songEditorLayerManager.IsVisible(note))
            .ToList();
        List<Note> notesInLayers = songEditorLayerManager.GetAllNotes();
        result.AddRange(notesInVoices);
        result.AddRange(notesInLayers);
        return result;
    }

    private void CreateVoiceToColorMap()
    {
        List<Color32> colors = new List<Color32> {
            ThemeManager.GetColor(R.Color.deviceColor_1),
            ThemeManager.GetColor(R.Color.deviceColor_2),
            ThemeManager.GetColor(R.Color.deviceColor_3),
            ThemeManager.GetColor(R.Color.deviceColor_4),
            ThemeManager.GetColor(R.Color.deviceColor_5),
            ThemeManager.GetColor(R.Color.deviceColor_6)
        };
        int index = 0;
        foreach (Voice v in SongMeta.GetVoices())
        {
            if (index < colors.Count)
            {
                voiceToColorMap[v] = colors[index];
            }
            else
            {
                // fallback color
                voiceToColorMap[v] = Colors.beige;
            }
            index++;
        }
    }

    public void OnBackButtonClicked()
    {
        ReturnToLastScene();
    }

    public void OnSaveButtonClicked()
    {
        SaveSong();
    }

    public void SaveSong()
    {
        string songFile = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Filename;

        // Create backup of original file if not done yet.
        if (SettingsManager.Instance.Settings.SongEditorSettings.SaveCopyOfOriginalFile)
        {
            CreateCopyOfFile(songFile);
        }

        try
        {
            // Write the song data structure to the file.
            UltraStarSongFileWriter.WriteFile(songFile, SongMeta);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            uiManager.CreateNotificationVisualElement("Saving the file failed:\n" + e.Message);
            return;
        }

        uiManager.CreateNotificationVisualElement("Saved file");
    }

    private void CreateCopyOfFile(string filePath)
    {
        try
        {
            string backupFile = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Filename.Replace(".txt", ".txt.bak");
            if (File.Exists(backupFile))
            {
                return;
            }
            File.Copy(filePath, backupFile);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            uiManager.CreateNotificationVisualElement("Creating a copy of the original file failed:\n" + e.Message);
            return;
        }

        uiManager.CreateNotificationVisualElement("Created copy of original file");
    }

    public void ContinueToSingScene()
    {
        SingSceneData singSceneData;
        if (sceneData.PreviousSceneData is SingSceneData)
        {
            singSceneData = sceneData.PreviousSceneData as SingSceneData;
        }
        else
        {
            singSceneData = new SingSceneData();
            singSceneData.SelectedSongMeta = sceneData.SelectedSongMeta;
            singSceneData.SelectedPlayerProfiles = sceneData.SelectedPlayerProfiles;
            singSceneData.PlayerProfileToMicProfileMap = sceneData.PlayerProfileToMicProfileMap;
        }
        singSceneData.PositionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
        SceneNavigator.Instance.LoadScene(EScene.SingScene, sceneData.PreviousSceneData);
    }

    public void ContinueToSongSelectScene()
    {
        SongSelectSceneData songSelectSceneData;
        if (sceneData.PreviousSceneData is SongSelectSceneData)
        {
            songSelectSceneData = sceneData.PreviousSceneData as SongSelectSceneData;
        }
        else
        {
            songSelectSceneData = new SongSelectSceneData();
            songSelectSceneData.SongMeta = sceneData.SelectedSongMeta;
        }
        SceneNavigator.Instance.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    public void ReturnToLastScene()
    {
        if (sceneData.PreviousSceneData is SingSceneData)
        {
            ContinueToSingScene();
            return;
        }
        ContinueToSongSelectScene();
    }
    
    public void ToggleAudioPlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            songAudioPlayer.PauseAudio();
        }
        else
        {
            songAudioPlayer.PlayAudio();
        }
    }
    
    public void StartEditingNoteText()
    {
        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.Count == 1)
        {
            Note selectedNote = selectedNotes.FirstOrDefault();
            EditorNoteControl noteControl = editorNoteDisplayer.GetNoteControl(selectedNote);
            if (noteControl != null)
            {
                noteControl.StartEditingNoteText();
            }
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        // Note that the SceneData and SongMeta are loaded on access here if not done yet.
        bb.BindExistingInstance(SceneData);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(noteAreaControl);
        bb.BindExistingInstance(songEditorLayerManager);
        bb.BindExistingInstance(micPitchTracker);
        bb.BindExistingInstance(songEditorNoteRecorder);
        bb.BindExistingInstance(selectionControl);
        bb.BindExistingInstance(lyricsAreaControl);
        bb.BindExistingInstance(editorNoteDisplayer);
        bb.BindExistingInstance(graphicRaycaster);
        bb.BindExistingInstance(historyManager);
        bb.BindExistingInstance(songMetaChangeEventStream);
        bb.BindExistingInstance(midiFileImporter);
        bb.BindExistingInstance(songEditorCopyPasteManager);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }
}
