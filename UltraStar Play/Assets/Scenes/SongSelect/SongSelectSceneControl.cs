using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneControl : MonoBehaviour, INeedInjection, IBinder, IInjectionFinishedListener
{
    private static readonly ProfilerMarker onInjectionFinishedProfilerMarker = new ProfilerMarker("SongSelectSceneControl.OnInjectionFinished");
    private readonly IComparer<object> songMetaPropertyComparer = new NullOrEmptyValueLastComparer();

    [InjectedInInspector]
    public SongSelectSceneInputControl songSelectSceneInputControl;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongRouletteControl songRouletteControl;

    [InjectedInInspector]
    public SongSelectSongPreviewControl songPreviewControl;

    [InjectedInInspector]
    public SongSelectPlayerListControl playerListControl;

    [InjectedInInspector]
    public NewestSamplesMicPitchTracker micPitchTrackerPrefab;

    [Inject]
    private DialogManager dialogManager;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.fuzzySearchTextLabel)]
    private Label fuzzySearchTextLabel;

    [Inject(UxmlName = R.UxmlNames.quitSceneButton)]
    private Button quitSceneButton;

    [Inject(UxmlName = R.UxmlNames.songOrderDropdownField)]
    private DropdownField songOrderDropdownField;

    [Inject]
    private AchievementEventStream achievementEventStream;

    [Inject]
    private SongSelectSceneData sceneData;

    [Inject]
    private Statistics statistics;

    [Inject(Optional = true)]
    private EventSystem eventSystem;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongIssueManager songIssueManager;

    [Inject(UxmlName = R.UxmlNames.noSongsFoundContainer)]
    private VisualElement noSongsFoundContainer;

    [Inject(UxmlName = R.UxmlNames.songScanInProgressContainer)]
    private VisualElement songScanInProgressContainer;

    [Inject(UxmlName = R.UxmlNames.songScanInProgressProgressLabel)]
    private Label songScanInProgressProgressLabel;

    [Inject(UxmlName = R.UxmlNames.importSongsButton)]
    private Button importSongsButton;

    [Inject(UxmlName = R.UxmlNames.showSearchExpressionInfoButton)]
    private Button showSearchExpressionInfoButton;

    [Inject(UxmlName = R.UxmlNames.searchExpressionToggle)]
    private Toggle searchExpressionToggle;

    [Inject(UxmlName = R.UxmlNames.micCheckToggle)]
    private Toggle micCheckToggle;

    [Inject(UxmlName = R.UxmlNames.selectRandomSongButton)]
    private Button selectRandomSongButton;

    private readonly SongSearchControl songSearchControl = new();

    public ReactiveProperty<int> RunningSongRepositorySearches { get; set; } = new(0);

    public SongMeta SelectedSong => (songRouletteControl.SelectedEntry as SongSelectSongEntry)?.SongMeta;

    private MessageDialogControl searchExpressionHelpDialogControl;
    private MessageDialogControl lyricsDialogControl;
    private MessageDialogControl noSingAlongDataDialogControl;
    private MessageDialogControl askToAssignMicsDialog;

    public PartyModeSceneData PartyModeSceneData => sceneData.partyModeSceneData;
    public bool HasPartyModeSceneData => PartyModeSceneData != null;
    public PartyModeSettings PartyModeSettings => PartyModeSceneData.PartyModeSettings;
    public bool IsPartyModeRandomSongSelection => HasPartyModeSceneData
                                                  && PartyModeSettings.SongSelectionSettings.SongSelectionMode == EPartyModeSongSelectionMode.Random;
    public bool UsePartyModePlaylist => IsPartyModeRandomSongSelection
                                        && PartyModeSettings.SongSelectionSettings.SongPoolPlaylistName != null;
    public bool CanUseSongSelectionJoker => PartyModeSceneData.remainingJokerCount != 0;

    public SongSelectionPlaylistChooserControl SongSelectionPlaylistChooserControl { get; private set; } = new();
    private readonly CreateSingAlongSongControl createSingAlongSongControl = new();
    private readonly SongSelectScenePartyModeControl partyModeControl = new();
    private readonly SongSelectFilterControl songSelectFilterControl = new();
    private readonly SongSelectSelectedSongDetailsControl songSelectSelectedSongDetailsControl = new();
    private readonly SongSelectMenuControl songSelectMenuControl = new();
    private readonly SongSelectSongQueueControl songSelectSongQueueControl = new();
    private readonly SongSelectModifiersControl songSelectModifiersControl = new();
    private readonly SongSelectDifficultyAndScoreModeControl songSelectDifficultyAndScoreModeControl = new();

    private readonly Subject<BeforeSongStartedEvent> beforeSongStartedEventStream = new();
    public IObservable<BeforeSongStartedEvent> BeforeSongStartedEventStream => beforeSongStartedEventStream;

    private DropdownFieldControl<ESongOrder> songOrderDropdownFieldControl;

    private List<SongMeta> lastSongMetasOfSongRouletteControl = new();
    private DirectoryInfo lastDirectoryInfoOfSongRouletteControl;
    private float lastSongMetaCountUpdateTimeInSeconds;
    private string lastRawSearchText;
    private SongSelectEntry selectedEntryBeforeSearch;

    public void OnInjectionFinished()
    {
        using IDisposable d = onInjectionFinishedProfilerMarker.Auto();

        injector.Inject(SongSelectionPlaylistChooserControl);
        injector.Inject(createSingAlongSongControl);
        injector.Inject(partyModeControl);
        injector.Inject(songSelectFilterControl);
        injector.Inject(songSearchControl);
        injector.Inject(songSelectSelectedSongDetailsControl);
        injector.Inject(songSelectMenuControl);
        injector.Inject(songSelectSongQueueControl);
        injector.Inject(songSelectModifiersControl);
        injector.Inject(songSelectDifficultyAndScoreModeControl);
    }

    private void Start()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.Start");

        songMetaManager.ScanSongsIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!songMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        songMetaManager.SongScanFinishedEventStream
            .Subscribe(evt =>
            {
                UpdateAvailableSongsAndUi(songMetaManager.IsSongScanFinished || evt != null);
            })
            .AddTo(gameObject);
        songMetaManager.AddedSongMetaEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 1000))
            .Subscribe(_ => UpdateAvailableSongsAndUi(songMetaManager.IsSongScanFinished));
        songMetaManager.RemovedSongMetaEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 1000))
            .Subscribe(_ => UpdateAvailableSongsAndUi(songMetaManager.IsSongScanFinished));
        UpdateSongScanLabels(songMetaManager.IsSongScanFinished);

        InitSongMetas();

        if (HasPartyModeSceneData
            && PartyModeSettings.SongSelectionSettings.SongSelectionMode == EPartyModeSongSelectionMode.Random)
        {
            partyModeControl.SelectRandomSong();
        }

        FieldBindingUtils.Bind(micCheckToggle,
            () => nonPersistentSettings.MicTestActive.Value,
            newValue => nonPersistentSettings.MicTestActive.Value = newValue);

        songOrderDropdownFieldControl = new(songOrderDropdownField, EnumUtils.GetValuesAsList<ESongOrder>(), settings.SongOrder,
            item => Translation.Get(item));
        songOrderDropdownFieldControl.SelectionAsObservable.Subscribe(newValue =>
        {
            Debug.Log($"New order: {newValue}");
            settings.SongOrder = newValue;
            UpdateFilteredSongs();
        });

        selectRandomSongButton.RegisterCallbackButtonTriggered(_ => SelectRandomSong());
        FieldBindingUtils.Bind(searchExpressionToggle,
            () => nonPersistentSettings.IsSearchExpressionsEnabled.Value,
            newValue => nonPersistentSettings.IsSearchExpressionsEnabled.Value = newValue);
        showSearchExpressionInfoButton.RegisterCallbackButtonTriggered(_ => ShowSearchExpressionHelp());

        fuzzySearchTextLabel.ShowByDisplay();
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(OnFuzzySearchTextChanged);

        songRouletteControl.SubmitEventStream.Subscribe(_ => OnSubmitSongRoulette());
        songRouletteControl.Focus();

        quitSceneButton.RegisterCallbackButtonTriggered(_ => QuitSongSelect());

        songSearchControl.SearchChangedEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => OnSearchTextChanged());
        songSearchControl.SubmitEventStream.Subscribe(_ => OnSubmitSearch());

        SongSelectionPlaylistChooserControl.Selection.Subscribe(_ => UpdateFilteredSongs());
        songSelectFilterControl.FiltersChangedEventStream.Subscribe(_ => UpdateFilteredSongs());

        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(it =>
            {
                if (settings.SongOrder is ESongOrder.LocalHighScore)
                {
                    UpdateFilteredSongs();
                }
            });

        playlistManager.PlaylistChangedEventStream.Subscribe(playlistChangeEvent =>
        {
            if (playlistChangeEvent.Playlist == SongSelectionPlaylistChooserControl.Selection.Value)
            {
                UpdateFilteredSongs();
            }
        });

        InitSongRoulette();

        importSongsButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.SongLibraryOptionsScene)));

        createSingAlongSongControl.CreatedSingAlongVersionEventStream.Subscribe(processedSongMeta =>
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_createdSingAlongDataSuccess,
                "name", Path.GetFileName(processedSongMeta.Audio)));
        });

        UpdateSceneTitle();
    }

    private void OnFuzzySearchTextChanged(string newValue)
    {
        if (!newValue.IsNullOrEmpty()
            && newValue.Length == 1)
        {
            // Single letter search jumps to the first song that matches current order property
            string songOrderTranslation = Translation.Get(settings.SongOrder);
            fuzzySearchTextLabel.SetTranslatedText(Translation.Of($"{songOrderTranslation}: {newValue}"));
            return;
        }

        fuzzySearchTextLabel.SetTranslatedText(Translation.Of(newValue));
    }

    private void UpdateAvailableSongsAndUi(bool isSongScanFinished)
    {
        InitSongMetas();
        UpdateFilteredSongs();
        UpdateSongScanLabels(isSongScanFinished);
    }

    public void QuitSongSelect()
    {
        if (HasPartyModeSceneData)
        {
            PartyModeSceneData partyModeSceneData = new();
            partyModeSceneData.PartyModeSettings = PartyModeSettings;
            sceneNavigator.LoadScene(EScene.PartyModeScene, partyModeSceneData);
        }
        else
        {
            sceneNavigator.LoadScene(EScene.MainScene);
        }
    }

    private void ShowSearchExpressionHelp()
    {
        ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_howToSearchExpressions));
    }

    public void ShowLyricsAndInfoPopup(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        if (lyricsDialogControl != null)
        {
            lyricsDialogControl.CloseDialog();
        }

        lyricsDialogControl = dialogManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_lyricsDialog_title,
            "songName", songMeta.Title));
        lyricsDialogControl.DialogClosedEventStream.Subscribe(_ => lyricsDialogControl = null);

        Label CreateLyricsLabel(string lyrics)
        {
            Label lyricsLabel = new Label(lyrics);
            lyricsLabel.enableRichText = true;
            lyricsLabel.AddToClassList("songSelectLyricsPreview");
            return lyricsLabel;
        }

        if (songMeta.VoiceCount < 2)
        {
            string lyrics = SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1);
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(lyrics));
        }
        else
        {
            string firstVoiceLyrics = $"<i><b>{songMeta.GetVoiceDisplayName(EVoiceId.P1)}</b></i>\n\n"
                                      + SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1);
            string secondVoiceLyrics = $"<i><b>{songMeta.GetVoiceDisplayName(EVoiceId.P2)}</b></i>\n\n"
                                       + SongMetaUtils.GetLyrics(songMeta, EVoiceId.P2);

            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(firstVoiceLyrics));
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(secondVoiceLyrics));
        }
    }

    public void InitSongMetas()
    {
        if (!songMetaManager.IsSongScanFinished)
        {
            return;
        }

        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongMetas");

        // Trigger achievement
        if (songMetaManager.GetSongMetas().Count > 100)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.browseMoreThan100Songs));
        }
    }

    private void UpdateSongScanLabels(bool isSongScanFinished)
    {
        songScanInProgressProgressLabel.SetTranslatedText(Translation.Of($"{songMetaManager.LoadedSongsPercent:00} %"));

        if (isSongScanFinished)
        {
            songScanInProgressContainer.HideByDisplay();
            noSongsFoundContainer.SetVisibleByDisplay(songMetaManager.LoadedSongsCount <= 0);
        }
        else
        {
            songScanInProgressContainer.ShowByDisplay();
            noSongsFoundContainer.HideByDisplay();
        }
    }

    private void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (!songMetaManager.IsSongScanFinished
            && Time.time - lastSongMetaCountUpdateTimeInSeconds > 1f )
        {
            lastSongMetaCountUpdateTimeInSeconds = Time.time;
            UpdateSongScanLabels(songMetaManager.IsSongScanFinished);
        }
    }

    private void InitSongRoulette()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongRouletteSongMetas");

        UpdateFilteredSongs();
        songRouletteControl.Selection.Subscribe(newValue => songSelectSelectedSongDetailsControl.OnSongSelectionChanged(newValue));
        songRouletteControl.SelectionClickedEventStream
            .Subscribe(_ => AttemptStartSelectedEntry());

        if (sceneData.SongMeta != null)
        {
            songRouletteControl.SelectEntryBySongMeta(sceneData.SongMeta);
        }
    }

    public void DoFuzzySearch(string text)
    {
        string searchTextNoWhitespace = text.Replace(" ", "");
        if (searchTextNoWhitespace.IsNullOrEmpty())
        {
            return;
        }

        // Try to jump to song-index
        if (TryExecuteSpecialSearchSyntax(text))
        {
            return;
        }

        SongSelectEntry entry = songSearchControl.GetFuzzySearchMatch(searchTextNoWhitespace);
        songRouletteControl.SelectEntry(entry);
    }

    private SingSceneData CreateSingSceneDataWithGivenSongAndSettings(SongMeta songMeta, bool startPaused)
    {
        SingSceneData singSceneData = new();
        singSceneData.SongMetas = new List<SongMeta> { songMeta };
        singSceneData.SingScenePlayerData = playerListControl.CreateSingScenePlayerData();
        singSceneData.partyModeSceneData = sceneData.partyModeSceneData;
        singSceneData.gameRoundSettings = new(nonPersistentSettings.GameRoundSettings);
        singSceneData.StartPaused = startPaused;

        if (singSceneData.gameRoundSettings != null
            && singSceneData.gameRoundSettings.modifiers.AnyMatch(modifier => modifier is ShortSongGameRoundModifier))
        {
            // Set as medley song to play shortened version
            // TODO: This modification should happen in SingScene where the GameRoundModifier behaviors are defined.
            singSceneData.MedleySongIndex = 0;
        }
        return singSceneData;
    }

    public void StartSingSceneWithGivenSongAndSettings(SongMeta songMeta, bool startPaused, bool fireBeforeSongStartedEvent)
    {
        if (HasFailedToLoadVoices(songMeta))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
            return;
        }

        if (fireBeforeSongStartedEvent)
        {
            BeforeSongStartedEvent evt = new(songMeta);
            beforeSongStartedEventStream.OnNext(evt);
            if (!evt.CancelReason.IsNullOrEmpty())
            {
                Log.Debug(() => $"Not starting song: {evt.CancelReason}");
                return;
            }
        }

        SingSceneData singSceneData = CreateSingSceneDataWithGivenSongAndSettings(songMeta, startPaused);
        if (singSceneData == null)
        {
            return;
        }

        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
    }

    private void StartSongEditorScene(SongMeta songMeta)
    {
        if (HasPartyModeSceneData)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.partyMode_error_notAvailable));
            return;
        }

        if (HasFailedToLoadVoices(songMeta))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason, "reason", "Failed to load txt file"));
            return;
        }

        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_error_audioNotFound));
            return;
        }

        SongEditorSceneData editorSceneData = new();
        editorSceneData.SongMeta = songMeta;

        SingSceneData singSceneData = CreateSingSceneDataWithGivenSongAndSettings(songMeta, false);
        if (singSceneData != null)
        {
            editorSceneData.PlayerProfileToMicProfileMap = singSceneData.SingScenePlayerData.PlayerProfileToMicProfileMap;
            editorSceneData.SelectedPlayerProfiles = singSceneData.SingScenePlayerData.SelectedPlayerProfiles;
        }
        editorSceneData.PreviousSceneData = sceneData;
        editorSceneData.PreviousScene = EScene.SongSelectScene;

        sceneNavigator.LoadScene(EScene.SongEditorScene, editorSceneData);
    }

    public void SelectRandomSong()
    {
        List<SongMeta> availableSongMetas = songRouletteControl.Entries
            .Where(entry => entry is SongSelectSongEntry)
            .Select(entry => (entry as SongSelectSongEntry).SongMeta)
            .Except(new List<SongMeta> { SelectedSong })
            .ToList();
        SongMeta randomSongMeta = RandomUtils.RandomOf(availableSongMetas);
        if (randomSongMeta == null)
        {
            return;
        }
        songRouletteControl.SelectEntryBySongMeta(randomSongMeta);
    }

    private async void CheckAudioThenStartSingScene(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        // Check that there is associated sing-along data. If not, ask to open song editor.
        if (!SongMetaUtils.HasSingAlongData(songMeta))
        {
            if (HasFailedToLoadVoices(songMeta))
            {
                ShowFailedToLoadVoicesDialog(songMeta);
                return;
            }

            ShowAskToCreateSingAlongDataDialog(songMeta);
            return;
        }

        // Check that the audio file exists
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            string audioUri = SongMetaUtils.GetAudioUri(songMeta);
            Translation errorMessage = Translation.Get(R.Messages.songSelectScene_error_audioNotFound,
                "name", audioUri);
            Debug.LogWarning(errorMessage);
            NotificationManager.CreateNotification(errorMessage);
            return;
        }

        // Check that the used audio format can be loaded.
        try
        {
            await songAudioPlayer.LoadAndPlayAsync(songMeta);
            StartSingSceneWithGivenSongAndSettings(songMeta, false, true);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError( $"Failed to load audio '{songMeta.GetArtistDashTitle()}': {ex.Message}");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_error_audioFailedToLoad,
                "name", songMeta.Audio,
                "supportedFormats", ApplicationUtils.supportedAudioFiles.JoinWith(", ")));
        }
    }

    private void ShowFailedToLoadVoicesDialog(SongMeta songMeta)
    {
        Translation errorMessage;
        if (songMeta is LazyLoadedVoicesSongMeta lazyLoadedVoicesSongMeta
            && !lazyLoadedVoicesSongMeta.FailedToLoadVoicesExceptionMessage.IsNullOrEmpty())
        {
            errorMessage = Translation.Of(lazyLoadedVoicesSongMeta.FailedToLoadVoicesExceptionMessage);
        }
        else
        {
            errorMessage = Translation.Empty;
        }

        dialogManager.CreateErrorInfoDialogControl(
            Translation.Get(R.Messages.songSelectScene_failedToLoadSongDialog_title),
            Translation.Get(R.Messages.songSelectScene_failedToLoadSongDialog_message),
            errorMessage);
    }

    private void ShowAskToCreateSingAlongDataDialog(SongMeta songMeta)
    {
        noSingAlongDataDialogControl = dialogManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_noSingAlongDataDialog_title));
        noSingAlongDataDialogControl.Message = Translation.Get(R.Messages.songSelectScene_noSingAlongDataDialog_message);
        noSingAlongDataDialogControl.MessageElement.AddToClassList("my-2");
        Button defaultButton = noSingAlongDataDialogControl.AddButton(Translation.Get(R.Messages.songSelectScene_noSingAlongDataDialog_createSingAlongData), _ =>
        {
            noSingAlongDataDialogControl.CloseDialog();
            createSingAlongSongControl.CreateSingAlongSong(songMeta, true);
        });
        noSingAlongDataDialogControl.AddButton(Translation.Get(R.Messages.action_openSongEditor), _ =>
        {
            noSingAlongDataDialogControl.CloseDialog();
            StartSongEditorScene(songMeta);
        });
        // noSingAlongDataDialogControl.AddButton("Start anyway", _ => StartSingScene(songMeta));
        noSingAlongDataDialogControl.AddButton(Translation.Get(R.Messages.action_cancel), _ => noSingAlongDataDialogControl.CloseDialog());
        defaultButton.Focus();
    }

    public void AttemptStartSelectedEntry()
    {
        AttemptStartEntry(songRouletteControl.SelectedEntry);
    }

    public void AttemptStartEntry(SongSelectEntry entry)
    {
        if (entry is SongSelectSongEntry songEntry)
        {
            AttemptStartSong(songEntry.SongMeta);
        }

        if (entry is SongSelectFolderEntry folderEntry)
        {
            NavigateIntoFolder(folderEntry.DirectoryInfo);
        }
    }

    private void NavigateIntoFolder(DirectoryInfo directoryInfo)
    {
        if (!settings.NavigateByFoldersInSongSelect)
        {
            return;
        }
        nonPersistentSettings.SongSelectDirectoryInfo = directoryInfo;
        UpdateFilteredSongs();

        // Restore selection
        if (nonPersistentSettings.SongSelectDirectoryPathToLastSelection.TryGetValue(directoryInfo.FullName, out string lastSelection))
        {
            songRouletteControl.SelectEntryByPath(lastSelection);
            songRouletteControl.FinishTransition();
        }
    }

    public bool TryNavigateToParentFolder()
    {
        if (!settings.NavigateByFoldersInSongSelect)
        {
            return false;
        }

        DirectoryInfo oldDirectoryInfo = nonPersistentSettings.SongSelectDirectoryInfo;
        if (oldDirectoryInfo == null
            || oldDirectoryInfo.Parent == null
            || SettingsUtils.IsSongFolderNavigationRootFolder(settings, oldDirectoryInfo))
        {
            return false;
        }

        // Select virtual root folder when configured song folder reached
        if (settings.SongDirs.AnyMatch(songFolder =>
                !settings.DisabledSongFolders.Contains(songFolder)
                && new DirectoryInfo(songFolder).FullName == oldDirectoryInfo.FullName))
        {
            nonPersistentSettings.SongSelectDirectoryInfo = new DirectoryInfo(SettingsUtils.SongFolderNavigationVirtualRootFolderName);
        }
        else
        {
            nonPersistentSettings.SongSelectDirectoryInfo = oldDirectoryInfo.Parent;
        }

        UpdateFilteredSongs();

        // Select entry of previous folder
        SongSelectEntry entryOfOldFolder = songRouletteControl.Entries
            .FirstOrDefault(entry => entry is SongSelectFolderEntry folderEntry
                                     && folderEntry.DirectoryInfo?.FullName == oldDirectoryInfo.FullName);
        if (entryOfOldFolder != null)
        {
            songRouletteControl.SelectEntry(entryOfOldFolder);
        }

        return true;
    }

    private void AttemptStartSong(SongMeta songMeta, bool ignoreRandomlySelectedSong = false, bool ignoreMissingMicProfiles = false)
    {
        List<PlayerProfile> selectedPlayerProfiles = playerListControl.GetSelectedPlayerProfiles();
        Dictionary<PlayerProfile, MicProfile> selectedPlayerProfileToMicProfileMap = playerListControl.GetSelectedPlayerProfileToMicProfileMap();

        // Check that any player is selected
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_noPlayerSelected_message));
            return;
        }

        // Ask to use joker if the user selected a different song than the randomly selected.
        if (!ignoreRandomlySelectedSong
            && IsPartyModeRandomSongSelection
            && partyModeControl.RandomlySelectedSong != songMeta)
        {
            // The user selected a different song than the randomly selected.
            // Ask to use joker or quit.
            if (CanUseSongSelectionJoker)
            {
                partyModeControl.OpenAskToUseJokerDialog(
                    songMeta,
                    () => AttemptStartSong(songMeta, true, ignoreMissingMicProfiles));
            }
            else
            {
                // No jokers left, go back to randomly selected song
                ShowCannotUseJokerMessage();
                songRouletteControl.SelectEntryBySongMeta(partyModeControl.RandomlySelectedSong);
            }
            return;
        }

        // Ask to connect a Companion App when there are players without mics.
        List<PlayerProfile> playerProfilesWithoutMics = selectedPlayerProfiles
            .Where(selectedPlayerProfile => CommonOnlineMultiplayerUtils.IsLocalPlayerProfile(selectedPlayerProfile))
            .Where(selectedPlayerProfile =>
                !selectedPlayerProfileToMicProfileMap.TryGetValue(selectedPlayerProfile, out MicProfile micProfile)
                || micProfile == null)
            .ToList();
        if (!ignoreMissingMicProfiles
            && !playerProfilesWithoutMics.IsNullOrEmpty())
        {
            OpenAskToAssignMicsDialog(
                playerProfilesWithoutMics,
                () => AttemptStartSong(songMeta, ignoreRandomlySelectedSong, true));
            return;
        }

        CheckAudioThenStartSingScene(songMeta);
    }

    private void OpenAskToAssignMicsDialog(List<PlayerProfile> playerProfilesWithoutMics, Action onIgnoreAndStart)
    {
        CloseAskToAssignMicsDialog();

        askToAssignMicsDialog = dialogManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_missingMicDialog_title));
        string playerNamesCsv = playerProfilesWithoutMics
            .Select(it => it.Name)
            .JoinWith(", ");
        askToAssignMicsDialog.Message = Translation.Get(R.Messages.songSelectScene_missingMicDialog_message,
            "playerNames", playerNamesCsv);
        askToAssignMicsDialog.AddButton(Translation.Get(R.Messages.songSelectScene_missingMicDialog_ignoreAndStart), _ =>
        {
            CloseAskToAssignMicsDialog();
            onIgnoreAndStart?.Invoke();
        });
        askToAssignMicsDialog.AddButton(Translation.Get(R.Messages.action_cancel), _ =>
        {
            CloseAskToAssignMicsDialog();
        });
    }

    private void CloseAskToAssignMicsDialog()
    {
        if (askToAssignMicsDialog == null)
        {
            return;
        }

        askToAssignMicsDialog.CloseDialog();
    }

    public void StartSongEditorScene()
    {
        if (SelectedSong != null)
        {
            StartSongEditorScene(SelectedSong);
        }
    }

    public void OnSearchTextChanged()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.OnSearchTextChanged");

        string rawSearchText = songSearchControl.GetRawSearchText();
        if (!rawSearchText.IsNullOrEmpty()
            && lastRawSearchText.IsNullOrEmpty())
        {
            // Remember selection from before search
            selectedEntryBeforeSearch = songRouletteControl.SelectedEntry;
        }
        lastRawSearchText = rawSearchText;

        StartSongRepositorySearch();

        if (TryExecuteSpecialSearchSyntax(rawSearchText))
        {
            // Special search syntax used. Do not perform normal filtering.
            return;
        }
        UpdateFilteredSongs();
    }

    private async void StartSongRepositorySearch()
    {
        string searchText = songSearchControl.GetSearchText();
        if (searchText.IsNullOrEmpty())
        {
            return;
        }
        Debug.Log($"StartSongRepositorySearch: searchText '{searchText}'");

        try
        {
            RunningSongRepositorySearches.Value++;
            SongRepositorySearchParameters searchParameters = new(searchText);
            List<SongRepositorySearchResult> searchResults = await SongRepositorySearcher.SearchSongsAsync(searchParameters);
            searchResults.SelectMany(result => result.Entries).ForEach(resultEntry => AddSearchResultEntryToSongMetaManager(resultEntry));
        }
        finally
        {
            RunningSongRepositorySearches.Value--;
        }
        UpdateFilteredSongs();
    }

    private void AddSearchResultEntryToSongMetaManager(SongRepositorySearchResultEntry searchResultEntry)
    {
        try
        {
            SongMeta songMeta = searchResultEntry.SongMeta;
            List<SongIssue> songIssues = searchResultEntry.SongIssues;
            if (songMeta != null
                && !songMetaManager.ContainsSongMeta(songMeta))
            {
                songMetaManager.AddSongMeta(songMeta);
                songIssueManager.AddSongIssues(songIssues);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.Log($"Failed to add search result entry '{searchResultEntry.SongMeta.GetArtistDashTitle()}'");
        }
    }

    private List<SongMeta> GetFilteredSongMetas()
    {
        IPlaylist playlist = SongSelectionPlaylistChooserControl.Selection.Value;
        bool PlaylistMatches(SongMeta songMeta)
        {
            return playlist == null
                   || playlist.HasSongEntry(songMeta);
        }

        bool ActiveFiltersMatches(SongMeta songMeta)
        {
            return songSelectFilterControl.SongMetaPassesActiveFilters(songMeta);
        }

        bool CurrentFolderMatches(SongMeta songMeta)
        {
            return nonPersistentSettings.SongSelectDirectoryInfo == null
                   // Typically each song has its own folder. Thus, show a song if its PARENT folder matches the selected folder.
                   || SongMetaUtils.GetDirectoryInfo(songMeta)?.Parent?.FullName == nonPersistentSettings.SongSelectDirectoryInfo.FullName
                   // Show songs that can not be shown otherwise in virtual root folder
                   || (SongMetaUtils.GetDirectoryInfo(songMeta) == null
                       && SettingsUtils.IsSongFolderNavigationRootFolder(settings, nonPersistentSettings.SongSelectDirectoryInfo));
        }

        List<SongMeta> filteredSongs = songSearchControl.GetFilteredSongMetas(songMetaManager.GetSongMetas())
            .Where(PlaylistMatches)
            .Where(ActiveFiltersMatches)
            .Where(CurrentFolderMatches)
            .OrderBy(songMeta => GetPrimarySongMetaOrderByProperty(songMeta), songMetaPropertyComparer)
            .ThenBy(songMeta => GetSecondarySongMetaOrderByProperty(songMeta), songMetaPropertyComparer)
            .ToList();
        return filteredSongs;
    }

    private object GetPrimarySongMetaOrderByProperty(SongMeta songMeta)
    {
        return GetSongMetaOrderByProperty(songMeta, settings.SongOrder);
    }

    private object GetSecondarySongMetaOrderByProperty(SongMeta songMeta)
    {
        ESongOrder secondaryOrderProperty = settings.SongOrder == ESongOrder.Title ? ESongOrder.Artist : ESongOrder.Title;
        return GetSongMetaOrderByProperty(songMeta, secondaryOrderProperty);
    }

    private object GetSongMetaOrderByProperty(SongMeta songMeta, ESongOrder songOrder)
    {
        switch (songOrder)
        {
            case ESongOrder.Artist:
                return songMeta.Artist;
            case ESongOrder.Title:
                return songMeta.Title;
            case ESongOrder.Genre:
                return songMeta.Genre;
            case ESongOrder.Language:
                return songMeta.Language;
            case ESongOrder.Folder:
                return SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
            case ESongOrder.Year:
                return songMeta.Year;
            case ESongOrder.LocalHighScore:
                // Return negative value to sort descending
                return -StatisticsUtils.GetLocalHighScore(statistics, songMeta, settings.Difficulty);
            case ESongOrder.CreationTime:
                return songMeta.FileInfo == null
                    ? 0
                    : -songMeta.FileInfo.CreationTimeUtc.Ticks;
            case ESongOrder.LastModificationTime:
                return songMeta.FileInfo == null
                    ? 0
                    : -songMeta.FileInfo.LastWriteTimeUtc.Ticks;
            default:
                Debug.LogWarning("Unknown order for songs: " + songOrderDropdownField.value);
                return songMeta.Artist;
        }
    }

    public void ToggleSelectedPlayers()
    {
        playerListControl.ToggleSelectedPlayers();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(SceneNavigator.GetSceneData(CreateDefaultSceneData()));
        bb.BindExistingInstance(songRouletteControl);
        bb.BindExistingInstance(songSelectSceneInputControl);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(playerListControl);
        bb.BindExistingInstance(SongSelectionPlaylistChooserControl);
        bb.BindExistingInstance(createSingAlongSongControl);
        bb.BindExistingInstance(partyModeControl);
        bb.BindExistingInstance(songSelectFilterControl);
        bb.BindExistingInstance(songPreviewControl);
        bb.BindExistingInstance(songSearchControl);
        bb.BindExistingInstance(songSelectSelectedSongDetailsControl);
        bb.BindExistingInstance(songSelectMenuControl);
        bb.BindExistingInstance(songSelectSongQueueControl);
        bb.BindExistingInstance(songSelectModifiersControl);
        bb.BindExistingInstance(songSelectDifficultyAndScoreModeControl);
        bb.Bind(nameof(micPitchTrackerPrefab)).ToExistingInstance(micPitchTrackerPrefab);
        return bb.GetBindings();
    }

    private SongSelectSceneData CreateDefaultSceneData()
    {
        return new SongSelectSceneData();
    }

    public void ToggleFavoritePlaylist()
    {
        SongSelectionPlaylistChooserControl.ToggleFavoritePlaylist();
    }

    public void ToggleSelectedSongIsFavorite()
    {
        if (SelectedSong == null)
        {
            return;
        }

        if (playlistManager.FavoritesPlaylist.HasSongEntry(SelectedSong))
        {
            playlistManager.RemoveSongFromPlaylist(playlistManager.FavoritesPlaylist, SelectedSong);
        }
        else
        {
            playlistManager.AddSongToPlaylist(playlistManager.FavoritesPlaylist, SelectedSong);
        }
    }

    private void UpdateFilteredSongs()
    {
        if (!songMetaManager.IsSongScanFinished)
        {
            return;
        }

        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.UpdateFilteredSongs");

        // Prepare to navigate by directory if needed
        UpdateSongSelectDirectoryInfo();

        // Check if filtered songs might have changed
        List<SongMeta> filteredSongMetas = GetFilteredSongMetas();
        if (!filteredSongMetas.IsNullOrEmpty()
            && filteredSongMetas.SequenceEqual(lastSongMetasOfSongRouletteControl)
            && lastDirectoryInfoOfSongRouletteControl == nonPersistentSettings.SongSelectDirectoryInfo)
        {
            return;
        }
        lastSongMetasOfSongRouletteControl = filteredSongMetas;
        lastDirectoryInfoOfSongRouletteControl = nonPersistentSettings.SongSelectDirectoryInfo;

        // Prepare to update entries
        List<SongSelectEntry> newEntries = new();

        // Add folder entries
        if (settings.NavigateByFoldersInSongSelect)
        {
            newEntries.AddRange(CreateSongSelectFolderEntries());
        }

        // Add song entries
        newEntries.AddRange(filteredSongMetas
            .Select(songMeta => new SongSelectSongEntry(songMeta) as SongSelectEntry));

        songRouletteControl.SetEntries(newEntries);
    }

    private List<SongSelectEntry> CreateSongSelectFolderEntries()
    {
        List<SongSelectEntry> entries = new();

        // Add entry to navigate to parent folder
        if (nonPersistentSettings.SongSelectDirectoryInfo.Name != SettingsUtils.SongFolderNavigationVirtualRootFolderName)
        {
            DirectoryInfo parentDirectoryInfo = SettingsUtils.IsSongFolderNavigationRootFolder(settings, nonPersistentSettings.SongSelectDirectoryInfo.Parent)
                    ? new DirectoryInfo(SettingsUtils.SongFolderNavigationVirtualRootFolderName)
                    : nonPersistentSettings.SongSelectDirectoryInfo.Parent;
            entries.Add(new SongSelectFolderEntry(parentDirectoryInfo));
        }

        // Add entries for other files and folders
        List<DirectoryInfo> directoryInfos = GetFilteredDirectoryInfos();
        entries.AddRange(directoryInfos
            .Select(directoryInfo => new SongSelectFolderEntry(directoryInfo)));

        return entries;
    }

    private void UpdateSongSelectDirectoryInfo()
    {
        if (settings.NavigateByFoldersInSongSelect
            && SettingsUtils.IsSongFolderNavigationRootFolder(settings, nonPersistentSettings.SongSelectDirectoryInfo))
        {
            nonPersistentSettings.SongSelectDirectoryInfo = new DirectoryInfo(SettingsUtils.SongFolderNavigationVirtualRootFolderName);
        }
        else if (!settings.NavigateByFoldersInSongSelect
                 && nonPersistentSettings.SongSelectDirectoryInfo != null)
        {
            nonPersistentSettings.SongSelectDirectoryInfo = null;
        }
    }

    private List<DirectoryInfo> GetFilteredDirectoryInfos()
    {
        if (nonPersistentSettings.SongSelectDirectoryInfo == null)
        {
            return new();
        }

        if (nonPersistentSettings.SongSelectDirectoryInfo.Name == SettingsUtils.SongFolderNavigationVirtualRootFolderName)
        {
            return settings.SongDirs
                .Where(songFolder => !settings.DisabledSongFolders.Contains(songFolder))
                .Select(songFolder => new DirectoryInfo(songFolder))
                .ToList();
        }

        // Typically each song has its own folder.
        // Thus, songs are shown if the PARENT folder matches the selected folder. This moves the songs one level up.
        // As a result, a folder should only be shown when it has further subfolders.
        List<DirectoryInfo> directoryInfos = nonPersistentSettings.SongSelectDirectoryInfo
            .EnumerateDirectories()
            .Where(childDirectoryInfo => childDirectoryInfo.EnumerateDirectories().Any())
            .ToList();

        string searchText = songSearchControl.GetSearchText();
        if (searchText.IsNullOrEmpty())
        {
            return directoryInfos;
        }

        return directoryInfos
            .Where(directoryInfo => directoryInfo.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    public bool IsPlaylistActive()
    {
        return SongSelectionPlaylistChooserControl.Selection.Value != null
               && !(SongSelectionPlaylistChooserControl.Selection.Value is UltraStarAllSongsPlaylist);
    }

    public void ResetPlaylistSelection()
    {
        SongSelectionPlaylistChooserControl.Reset();
    }

    private bool TryExecuteSpecialSearchSyntax(string searchText)
    {
        if (IsPartyModeRandomSongSelection)
        {
            return false;
        }

        if (searchText != null && searchText.StartsWith("#"))
        {
            // #<number> jumps to song at index <number>.
            // The check for the special syntax has already been made, so we know the searchText starts with #.
            string numberString = searchText.Substring(1);
            if (int.TryParse(numberString, out int number))
            {
                songRouletteControl.SelectEntryByIndex(number - 1, false);
                return true;
            }
        }
        return false;
    }

    public void UpdateSceneTitle()
    {
        if (HasPartyModeSceneData)
        {
            sceneTitle.SetTranslatedText(Translation.Of(
                $"{Translation.Get(R.Messages.songSelectScene_title)}\n"
                + $"{PartyModeSceneData.currentRoundIndex + 1} / {PartyModeSettings.RoundCount}"));
        }
    }

    private void OnSubmitSongRoulette()
    {
        AttemptStartSelectedEntry();
    }

    private void OnSubmitSearch()
    {
        songSearchControl.ResetSearchText();
        songRouletteControl.Focus();

        // Continue browsing songs from the currently selected entry.
        selectedEntryBeforeSearch = null;
    }

    public void OnCancelSearch()
    {
        songSearchControl.ResetSearchText();
        songRouletteControl.Focus();

        // Continue browsing songs from the entry that was selected before starting the search.
        songRouletteControl.SelectEntry(selectedEntryBeforeSearch);
        selectedEntryBeforeSearch = null;
    }

    public void ShowCannotUseJokerMessage()
    {
        NotificationManager.CreateNotification(Translation.Get(R.Messages.partyMode_error_noJokers));
    }

    public List<PlayerProfile> GetEnabledPlayerProfiles()
    {
        if (!HasPartyModeSceneData)
        {
            return SettingsUtils.GetPlayerProfiles(settings, nonPersistentSettings)
                .Where(playerProfile => playerProfile.IsEnabled)
                .ToList();
        }
        else if (PartyModeSettings.TeamSettings.IsFreeForAll)
        {
            // Select all players of all teams
            List<PlayerProfile> allPlayerProfiles = PartyModeUtils.GetAllPlayerProfiles(PartyModeSettings);
            return allPlayerProfiles
                .Where(playerProfile => playerProfile != null
                                        && !PartyModeUtils.IsKnockedOut(PartyModeSceneData, PartyModeUtils.GetTeam(PartyModeSceneData, playerProfile)))
                .Distinct()
                .ToList();
        }
        else
        {
            // Select random player of each team
            List<PlayerProfile> result = new();
            PartyModeSettings.TeamSettings.Teams
                .Where(team => !PartyModeUtils.IsKnockedOut(PartyModeSceneData, team))
                .ForEach(team =>
                {
                    List<PlayerProfile> allTeamPlayerProfiles = team.playerProfiles.Union(team.guestPlayerProfiles).ToList();
                    PlayerProfile playerProfile = RandomUtils.RandomOf(allTeamPlayerProfiles);
                    result.Add(playerProfile);
                });
            return result
                .Where(playerProfile => playerProfile != null)
                .Distinct()
                .ToList();
        }
    }

    public void AskToRecreateSingAlongData(SongMeta songMeta)
    {
        MessageDialogControl dialogControl = dialogManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_recreateSongDialog_title, "songName", songMeta.Title));
        dialogControl.Message = Translation.Get(R.Messages.songSelectScene_recreateSongDialog_message);
        dialogControl.AddButton(Translation.Get(R.Messages.songSelectScene_recreateSongDialog_recreateAndSave), evt =>
        {
            dialogControl.CloseDialog();
            createSingAlongSongControl.CreateSingAlongSong(songMeta, true);
        });
        dialogControl.AddButton(Translation.Get(R.Messages.songSelectScene_recreateSongDialog_recreateButDoNotSave), evt =>
        {
            dialogControl.CloseDialog();
            createSingAlongSongControl.CreateSingAlongSong(songMeta, false);
        });
        dialogControl.AddButton(Translation.Get(R.Messages.action_cancel), evt =>
        {
            dialogControl.CloseDialog();
        });

        dialogControl.AddInformationMessage($"AI model parameters can be changed in the song editor");
    }

    private static bool HasFailedToLoadVoices(SongMeta songMeta)
    {
        return songMeta is LazyLoadedVoicesSongMeta lazyLoadedVoicesSongMeta
               && lazyLoadedVoicesSongMeta.LoadVoicesPhase is LazyLoadedVoicesSongMeta.ELoadVoicesPhase.Failed;
    }
}
