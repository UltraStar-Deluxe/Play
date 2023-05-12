using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
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
    public SongEditorSelectionControl selectionControl;

    [InjectedInInspector]
    public EditorNoteDisplayer editorNoteDisplayer;

    [InjectedInInspector]
    public SongEditorHistoryManager historyManager;

    [InjectedInInspector]
    public SongEditorLayerManager songEditorLayerManager;

    [InjectedInInspector]
    public SongEditorCopyPasteManager songEditorCopyPasteManager;

    [InjectedInInspector]
    public SongEditorSceneInputControl songEditorSceneInputControl;

    [InjectedInInspector]
    public SongEditorAlternativeAudioPlayer songEditorAlternativeAudioPlayer;

    [InjectedInInspector]
    public SongEditorMicSampleRecorder songEditorMicSampleRecorder;
    
    [InjectedInInspector]
    public StyleSheet songEditorSmallScreenStyleSheet;
    
    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.editLyricsPopup)]
    private VisualElement editLyricsPopup;

    [Inject(UxmlName = R.UxmlNames.rightSideBar)]
    private VisualElement rightSideBar;

    [Inject]
    private ApplicationManager applicationManager;
    
    [Inject]
    private AchievementEventStream achievementEventStream;

    private IDisposable autoSaveDisposable;

    private readonly SongMetaChangeEventStream songMetaChangeEventStream = new();

    private double positionInSongInMillisWhenPlaybackStarted;

    private readonly Dictionary<string, Color> voiceNameToColorMap = new();

    private bool audioWaveFormInitialized;

    public double StopPlaybackAfterPositionInSongInMillis { get; set; }

    private readonly OverviewAreaControl overviewAreaControl = new();
    private readonly VideoAreaControl videoAreaControl = new();
    private readonly SongEditorVirtualPianoControl songEditorVirtualPianoControl = new();
    private readonly LyricsAreaControl lyricsAreaControl = new();
    private readonly NoteAreaControl noteAreaControl = new();
    private readonly SongEditorSideBarControl sideBarControl = new();
    private readonly SongEditorIssueAnalyzerControl issueAnalyzerControl = new();
    private readonly SongEditorStatusBarControl statusBarControl = new();
    private readonly SongEditorBackgroundAudioWaveFormControl songEditorBackgroundAudioWaveFormControl = new();
    private readonly SongEditorSearchControl songEditorSearchControl = new();
    private readonly ImportLrcDialogControl importLrcDialogControl = new();

    [Inject]
    private SongEditorSceneData sceneData;
    private SongMeta SongMeta => sceneData.SongMeta;

    private readonly List<IDialogControl> openDialogControls = new();
    public bool IsAnyDialogOpen => openDialogControls.Count > 0;

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
        injector.Inject(songEditorBackgroundAudioWaveFormControl);
        injector.Inject(songEditorSearchControl);
        injector.Inject(importLrcDialogControl);
        injector
            .WithRootVisualElement(rightSideBar)
            .CreateAndInject<DragToChangeRightSideBarWidthControl>();
    }

    private void Start()
    {
        Debug.Log($"Start editing of '{SongMeta.Title}' at {sceneData.PositionInSongInMillis} ms.");

        InitSongEditorStyleSheet();
        
        songAudioPlayer.Init(SongMeta);

        songAudioPlayer.PlaybackStartedEventStream
            .Subscribe(positionInSongInMillis => OnAudioPlaybackStarted(positionInSongInMillis));
        songAudioPlayer.PlaybackStoppedEventStream
            .Subscribe(_ => OnAudioPlaybackStopped());

        songVideoPlayer.SongMeta = SongMeta;

        HideEditLyricsPopup();

        if (uiDocument.rootVisualElement.focusController.focusedElement != null)
        {
            uiDocument.rootVisualElement.focusController.focusedElement.Blur();
        }

        InitAutoSave();

        InitSteamAchievement();
    }

    private void InitSongEditorStyleSheet()
    {
        uiDocument.rootVisualElement.AddToClassList(R.UssClasses.songEditorRoot);
        
        if (ApplicationUtils.IsSmallScreen()
            && songEditorSmallScreenStyleSheet != null)
        {
            uiDocument.rootVisualElement.styleSheets.Add(songEditorSmallScreenStyleSheet);
        }
    }

    private void InitSteamAchievement()
    {
        songMetaChangeEventStream
            .Subscribe(evt =>
            {
                if (evt is NotesChangedEvent)
                {
                    achievementEventStream.OnNext(AchievementId.editNotesInSongEditor);
                }
            })
            .AddTo(gameObject);
    }

    private void OnDestroy()
    {
        videoAreaControl.Dispose();
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

        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ => DoAutoSaveIfEnabled())
            .AddTo(gameObject);
    }

    private void RegisterAutoSaveEvent()
    {
        UnregisterAutoSaveEvent();

        autoSaveDisposable = songMetaChangeEventStream
            // When there has been no new event for a second, then save
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
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

    private void OnAudioPlaybackStopped()
    {
        // Go to last position in song when playback stopped
        bool invertedGoToLastPlaybackPositionBehavior = InputUtils.IsKeyboardControlPressed();
        bool goToLastPlaybackPosition = (settings.SongEditorSettings.GoToLastPlaybackPosition &&
                                         !invertedGoToLastPlaybackPositionBehavior)
                                        || (!settings.SongEditorSettings.GoToLastPlaybackPosition &&
                                            invertedGoToLastPlaybackPositionBehavior);
        if (goToLastPlaybackPosition)
        {
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillisWhenPlaybackStarted;
        }
    }

    private void OnAudioPlaybackStarted(double positionInSongInMillis)
    {
        positionInSongInMillisWhenPlaybackStarted = positionInSongInMillis;
    }

    public List<Note> GetAllVisibleNotes()
    {
        List<Note> result = new();
        List<Note> notesInVoices = SongMeta.GetVoices()
            // Second voice is drawn on top of first voice. Thus, start with second voice.
            .Reverse()
            .SelectMany(voice => voice.Sentences)
            .SelectMany(sentence => sentence.Notes)
            .Where(note => songEditorLayerManager.IsNoteVisible(note))
            .ToList();
        List<Note> notesInLayers = songEditorLayerManager.GetAllEnumLayerNotes();
        result.AddRange(notesInLayers);
        result.AddRange(notesInVoices);
        return result;
    }

    private void CreateVoiceToColorMap()
    {
        List<Color> colors = new()
        {
            Colors.CreateColor("#"),
            Colors.CreateColor("#"),
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

        songMetaManager.SaveSong(SongMeta, settings.SongEditorSettings.AutoSave);
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
            singSceneData.SongMetas = new List<SongMeta> { sceneData.SongMeta };
            singSceneData.SingScenePlayerData.SelectedPlayerProfiles = sceneData.SelectedPlayerProfiles;
            singSceneData.SingScenePlayerData.PlayerProfileToMicProfileMap = sceneData.PlayerProfileToMicProfileMap;
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
        }
        songSelectSceneData.SongMeta = sceneData.SongMeta;
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
    
    public void StartEditingSelectedNoteText()
    {
        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.Count == 1)
        {
            Note selectedNote = selectedNotes.FirstOrDefault();
            EditorNoteControl noteControl = editorNoteDisplayer.GetNoteControl(selectedNote);
            if (noteControl != null)
            {
                noteControl.StartEditingLyrics();
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

        VisualElement visualElement = valueInputDialogUi.CloneTree();
        visualElement.AddToClassList("overlay");
        uiDocument.rootVisualElement.Add(visualElement);

        TextInputDialogControl dialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<TextInputDialogControl>();
        dialogControl.Title = title;
        dialogControl.Message = message;

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
                UiManager.CreateNotification($"File does not exist");
            }
            usePathCallback(path);
        }

        VisualElement visualElement = valueInputDialogUi.CloneTree();
        visualElement.AddToClassList("overlay");
        uiDocument.rootVisualElement.Add(visualElement);

        PathInputDialogControl dialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<PathInputDialogControl>();
        dialogControl.Title = title;
        dialogControl.Message = message;
        dialogControl.InitialValue = initialValue;

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
        SongEditorSceneData songEditorSceneData = SceneNavigator.GetSceneDataOrThrow<SongEditorSceneData>();

        BindingBuilder bb = new();
        // Note that the SceneData and SongMeta are loaded on access here if not done yet.
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(songEditorSceneData);
        bb.BindExistingInstance(songEditorSceneData.SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(noteAreaControl);
        bb.BindExistingInstance(songEditorLayerManager);
        bb.BindExistingInstance(songEditorMicSampleRecorder);
        bb.BindExistingInstance(importLrcDialogControl);
        bb.BindExistingInstance(songEditorAlternativeAudioPlayer);
        bb.BindExistingInstance(selectionControl);
        bb.BindExistingInstance(songEditorSearchControl);
        bb.BindExistingInstance(lyricsAreaControl);
        bb.BindExistingInstance(editorNoteDisplayer);
        bb.BindExistingInstance(sideBarControl);
        bb.BindExistingInstance(historyManager);
        bb.BindExistingInstance(overviewAreaControl);
        bb.BindExistingInstance(songMetaChangeEventStream);
        bb.BindExistingInstance(songEditorCopyPasteManager);
        bb.BindExistingInstance(songEditorSceneInputControl);
        bb.BindExistingInstance(issueAnalyzerControl);
        bb.BindExistingInstance(statusBarControl);
        bb.BindExistingInstance(this);
        bb.Bind(nameof(issueSideBarEntryUi)).ToExistingInstance(issueSideBarEntryUi);
        bb.Bind(nameof(songPropertySideBarEntryUi)).ToExistingInstance(songPropertySideBarEntryUi);
        bb.Bind(nameof(songEditorLayerSideBarEntryUi)).ToExistingInstance(songEditorLayerSideBarEntryUi);
        return bb.GetBindings();
    }
}
