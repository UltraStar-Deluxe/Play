using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorSceneControl : MonoBehaviour, IBinder, INeedInjection, IInjectionFinishedListener
{
    [InjectedInInspector]
    public VisualTreeAsset issueSideBarEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset songPropertySideBarEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset songEditorLayerSideBarEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset valueInputDialogUi;

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
    public SongEditorHistoryManager historyManager;

    [InjectedInInspector]
    public SongEditorLayerManager songEditorLayerManager;

    [InjectedInInspector]
    public SongEditorCopyPasteManager songEditorCopyPasteManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.editLyricsPopup)]
    private VisualElement editLyricsPopup;

    private IDisposable autoSaveDisposable;

    private readonly SongMetaChangeEventStream songMetaChangeEventStream = new SongMetaChangeEventStream();

    private double positionInSongInMillisWhenPlaybackStarted;

    private readonly Dictionary<string, Color> voiceNameToColorMap = new Dictionary<string, Color>();

    private bool audioWaveFormInitialized;

    public double StopPlaybackAfterPositionInSongInMillis { get; set; }

    private readonly OverviewAreaControl overviewAreaControl = new OverviewAreaControl();
    private readonly VideoAreaControl videoAreaControl = new VideoAreaControl();
    private readonly SongEditorVirtualPianoControl songEditorVirtualPianoControl = new SongEditorVirtualPianoControl();
    private readonly LyricsAreaControl lyricsAreaControl = new LyricsAreaControl();
    private readonly NoteAreaControl noteAreaControl = new NoteAreaControl();
    private readonly SongEditorSideBarControl sideBarControl = new SongEditorSideBarControl();
    private readonly SongEditorIssueAnalyzerControl issueAnalyzerControl = new SongEditorIssueAnalyzerControl();
    private readonly SongEditorStatusBarControl statusBarControl = new SongEditorStatusBarControl();

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
                // Use of SceneNavigator.Instance because injection might not have been completed yet.
                sceneData = SceneNavigator.Instance.GetSceneDataOrThrow<SongEditorSceneData>();
            }
            return sceneData;
        }
    }

    private readonly List<IDialogControl> openDialogControls = new List<IDialogControl>();
    public bool IsAnyDialogOpen => openDialogControls.Count > 0;

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
        injector.Inject(sideBarControl);
        injector.Inject(issueAnalyzerControl);
        injector.Inject(statusBarControl);
    }

    private void Start()
    {
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(OnAudioPlaybackStarted);
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(OnAudioPlaybackStopped);

        HideEditLyricsPopup();

        if (uiDocument.rootVisualElement.focusController.focusedElement != null)
        {
            uiDocument.rootVisualElement.focusController.focusedElement.Blur();
        }

        InitAutoSave();
    }

    private void InitAutoSave()
    {
        if (settings.SongEditorSettings.AutoSave)
        {
            RegisterAutoSaveEvent();
        }

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.AutoSave)
            .Subscribe(autoSave =>
            {
                if (autoSave)
                {
                    RegisterAutoSaveEvent();
                }
                else
                {
                    UnregisterAutoSaveEvent();
                }
            })
            .AddTo(gameObject);

        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => DoAutoSaveIfEnabled());
    }

    private void RegisterAutoSaveEvent()
    {
        UnregisterAutoSaveEvent();

        autoSaveDisposable = songMetaChangeEventStream
            // When there has been no new event for a second, then save
            .Throttle(new TimeSpan(0, 0, 0, 0, 1000))
            .Subscribe(evt => DoAutoSaveIfEnabled())
            .AddTo(gameObject);
    }

    private void UnregisterAutoSaveEvent()
    {
        if (autoSaveDisposable != null)
        {
            autoSaveDisposable.Dispose();
            autoSaveDisposable = null;
        }
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
        string voiceName = voice.Name == Voice.soloVoiceName
            ? Voice.firstVoiceName
            : voice.Name;
        return GetColorForVoiceName(voiceName);
    }

    public Color GetColorForVoiceName(string voiceName)
    {
        if (voiceNameToColorMap.TryGetValue(voiceName, out Color color))
        {
            return color;
        }
        else
        {
            // Define colors for the voices.
            CreateVoiceToColorMap();
            return voiceNameToColorMap[voiceName];
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
        List<Color> colors = new List<Color> {
            Colors.CreateColor("#2ecc71"),
            Colors.CreateColor("#9b59b6"),
        };
        int index = 0;
        foreach (Color color in colors)
        {
            string voiceName = "P" + (index + 1);
            voiceNameToColorMap[voiceName] = colors[index];
            index++;
        }
    }

    private void DoAutoSaveIfEnabled()
    {
        if (!settings.SongEditorSettings.AutoSave)
        {
            return;
        }

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

    public void RestartSongEditorScene()
    {
        sceneNavigator.LoadScene(EScene.SongEditorScene, sceneData);
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
        sceneNavigator.LoadScene(EScene.SingScene, sceneData.PreviousSceneData);
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
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
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

    public void CreateNumberInputDialog(string title, string message, Action<float> useNumberCallback)
    {
        void UseValueCallback(string text)
        {
            text = text.Trim();
            if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out float numberValue))
            {
                useNumberCallback(numberValue);
            }
        }

        TextInputDialogControl dialogControl = new TextInputDialogControl(
            valueInputDialogUi,
            uiDocument.rootVisualElement.Children().First(),
            title,
            message,
            "");

        dialogControl.SubmitValueEventStream
            .Subscribe(newValue => UseValueCallback(newValue));

        openDialogControls.Add(dialogControl);
        dialogControl.DialogClosedEventStream
            .Subscribe(_ => openDialogControls.Remove(dialogControl));
    }

    public void CreatePathInputDialog(
        string title,
        string message,
        string initialValue,
        Action<string> usePathCallback)
    {
        void UseValueCallback(string path)
        {
            path = path.Trim();
            if (!File.Exists(path))
            {
                Debug.Log($"File does not exist: {path}");
                uiManager.CreateNotificationVisualElement($"File does not exist");
            }
            usePathCallback(path);
        }

        PathInputDialogControl dialogControl = new PathInputDialogControl(
            valueInputDialogUi,
            uiDocument.rootVisualElement.Children().First(),
            title,
            message,
            initialValue);

        dialogControl.SubmitValueEventStream
            .Subscribe(newValue => UseValueCallback(newValue));

        openDialogControls.Add(dialogControl);
        dialogControl.DialogClosedEventStream
            .Subscribe(_ => openDialogControls.Remove(dialogControl));
    }

    public void CloseAllOpenDialogs()
    {
        openDialogControls
            .ToList()
            .ForEach(it => it.CloseDialog());
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
        bb.BindExistingInstance(sideBarControl);
        bb.BindExistingInstance(historyManager);
        bb.BindExistingInstance(songMetaChangeEventStream);
        bb.BindExistingInstance(songEditorCopyPasteManager);
        bb.BindExistingInstance(issueAnalyzerControl);
        bb.BindExistingInstance(statusBarControl);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        bb.Bind(nameof(issueSideBarEntryUi)).ToExistingInstance(issueSideBarEntryUi);
        bb.Bind(nameof(songPropertySideBarEntryUi)).ToExistingInstance(songPropertySideBarEntryUi);
        bb.Bind(nameof(songEditorLayerSideBarEntryUi)).ToExistingInstance(songEditorLayerSideBarEntryUi);
        return bb.GetBindings();
    }
}
